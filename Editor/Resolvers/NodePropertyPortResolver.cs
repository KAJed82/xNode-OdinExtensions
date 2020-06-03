using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using UnityEditor;
using UnityEngine;

using XNode;
using XNode.Odin;
using static XNode.Node;

namespace XNodeEditor.Odin
{
	public interface INodePortResolver
	{
		Node Node { get; }

		NodePortInfo GetNodePortInfo( string propertyName );

		void RememberDynamicPort( InspectorProperty property );
		void ForgetDynamicPort( InspectorProperty property );
	}

	public class NodePortInfo
	{
		private ConnectionType _connectionType;
		private TypeConstraint _typeConstraint;

		public InspectorPropertyInfo PortPropertyInfo { get; private set; }
		public InspectorPropertyInfo SourcePropertyInfo { get; private set; }

		public string BaseFieldName { get; private set; }

		public Type Type { get; private set; }

		public Node Node { get; private set; }
		public NodePort Port => Node.GetPort( BaseFieldName );

		public ShowBackingValue ShowBackingValue { get; private set; }
		public ConnectionType ConnectionType { get => Port == null ? _connectionType : Port.connectionType; private set => _connectionType = value; }
		public TypeConstraint TypeConstraint { get => Port == null ? _typeConstraint : Port.typeConstraint; private set => _typeConstraint = value; }

		public bool IsDynamicPortList { get; private set; }
		public bool IsDynamic { get; private set; }

		public bool HasValue { get; }

		public bool IsInput { get; private set; }

		public NodePortInfo(
			InspectorPropertyInfo portPropertyInfo,
			InspectorPropertyInfo sourcePropertyInfo,
			string baseFieldName,
			Type type,
			Node node, // Needed?
			ShowBackingValue showBackingValue,
			ConnectionType connectionType,
			TypeConstraint typeConstraint,
			bool isDynamicPortList,
			bool isDynamic,
			bool isInput,
			bool hasValue
		)
		{
			PortPropertyInfo = portPropertyInfo;
			SourcePropertyInfo = sourcePropertyInfo;
			BaseFieldName = baseFieldName;
			Type = type;
			Node = node;
			ShowBackingValue = showBackingValue;
			ConnectionType = connectionType;
			TypeConstraint = typeConstraint;
			IsDynamicPortList = isDynamicPortList;
			IsDynamic = isDynamic;
			IsInput = isInput;
			HasValue = hasValue;
		}
	}

	// Invert the pattern
	// Inject this property into a node port holder
	[ResolverPriority( 10 )]
	public abstract class NodePropertyPortResolver<TValue> : OdinPropertyResolver<TValue>, IRefreshableResolver, IDisposable, INodePortResolver
	{
		#region Taken from BaseMemberPropertyResolver
		private List<InspectorPropertyInfo> infos;
		private Dictionary<string, int> namesToIndex;

		public sealed override InspectorPropertyInfo GetChildInfo( int childIndex )
		{
			if ( object.ReferenceEquals( this.infos, null ) )
			{
				this.LazyInitialize();
			}

			return this.infos[childIndex];
		}

		public sealed override int ChildNameToIndex( string name )
		{
			if ( object.ReferenceEquals( this.infos, null ) )
			{
				this.LazyInitialize();
			}

			int result;
			if ( this.namesToIndex.TryGetValue( name, out result ) ) return result;
			return -1;
		}

		protected sealed override int GetChildCount( TValue value )
		{
			if ( object.ReferenceEquals( this.infos, null ) )
			{
				this.LazyInitialize();
			}

			return this.infos.Count;
		}

		private bool initializing;

		private void LazyInitialize()
		{
			if ( this.initializing )
				throw new Exception( "Illegal API call was made: cannot query members of a property that are dependent on children being initialized, during the initialization of the property's children." );

			this.initializing = true;
			this.infos = this.GetPropertyInfos().ToList();
			this.initializing = false;

			this.namesToIndex = new Dictionary<string, int>();

			for ( int i = 0; i < infos.Count; i++ )
			{
				var info = infos[i];
				this.namesToIndex[info.PropertyName] = i;
			}
		}
		#endregion

		protected static Regex s_DynamicPortRegex = new Regex( @"^(.+) (\d)$" );

		public override bool CanResolveForPropertyFilter( InspectorProperty property )
		{
			if ( !NodeEditor.InNodeEditor )
				return false;

			return base.CanResolveForPropertyFilter( property );
		}

		private List<OdinPropertyProcessor> processors;

		public virtual void Dispose()
		{
			if ( this.processors != null )
			{
				for ( int i = 0; i < this.processors.Count; i++ )
				{
					var disposable = this.processors[i] as IDisposable;

					if ( disposable != null )
					{
						disposable.Dispose();
					}
				}
			}

			EditorApplication.update -= Update;
		}

		public Node Node { get; private set; }

		protected Dictionary<string, NodePortInfo> nameToNodePropertyInfo = new Dictionary<string, NodePortInfo>();
		protected Dictionary<string, string> propertyToNodeProperty = new Dictionary<string, string>();

		protected DisplayDynamicPortsAttribute displayDynamicPortsAttribute;

		public NodePortInfo GetNodePortInfo( string propertyName )
		{
			if ( propertyToNodeProperty.TryGetValue( propertyName, out var portPropertyName ) )
			{
				if ( nameToNodePropertyInfo.TryGetValue( portPropertyName, out var nodePortInfo ) )
					return nodePortInfo;
			}

			return null;
		}

		protected override void Initialize()
		{
			base.Initialize();

			Node = Property.Tree.WeakTargets.FirstOrDefault() as Node;
		}

		protected InspectorPropertyInfo[] GetPropertyInfos()
		{
			if ( this.processors == null )
			{
				this.processors = OdinPropertyProcessorLocator.GetMemberProcessors( this.Property );
			}

			var includeSpeciallySerializedMembers = this.Property.ValueEntry.SerializationBackend != SerializationBackend.Unity;
			var infos = InspectorPropertyInfoUtility.CreateMemberProperties( this.Property, typeof( TValue ), includeSpeciallySerializedMembers );

			// If we resolve the ports from the port dictionary i might be able to communicate between properties
			// in order to make dynamic port adding cleaner

			// Resolve my own members so I can see them
#if DEBUG_RESOLVER
			infos.AddValue(
				$"resolver:{nameof(infos)}",
				() => this.infos,
				value => { }
			);

			infos.AddValue(
				$"resolver:{nameof( namesToIndex )}",
				() => this.namesToIndex,
				value => { }
			);

			infos.AddValue(
				$"resolver:{nameof( nameToNodePropertyInfo )}",
				() => this.nameToNodePropertyInfo,
				value => { }
			);

			infos.AddValue(
				$"resolver:{nameof( propertyToNodeProperty )}",
				() => this.propertyToNodeProperty,
				value => { }
			);
#endif

			LabelWidthAttribute labelWidthAttribute = Property.GetAttribute<LabelWidthAttribute>();
			displayDynamicPortsAttribute = Property.GetAttribute<DisplayDynamicPortsAttribute>();

			// Port makers
			{
				for ( int i = 0; i < infos.Count; ++i )
				{
					var info = infos[i];
					if ( labelWidthAttribute != null )
					{
						if ( info.GetAttribute<LabelWidthAttribute>() == null )
							info.GetEditableAttributesList().Add( labelWidthAttribute );
					}

					var inputAttribute = info.GetMemberInfo().GetAttribute<InputAttribute>();
					var outputAttribute = info.GetMemberInfo().GetAttribute<OutputAttribute>();
					if ( inputAttribute != null || outputAttribute != null ) // Make a port.... we'll deal with dynamic later
					{
						string baseFieldName = info.PropertyName;
						NodePort port = Node.GetPort( info.PropertyName );
						ShowBackingValue showBackingValue = ShowBackingValue.Always;
						ConnectionType connectionType = ConnectionType.Multiple;
						TypeConstraint typeConstraint = TypeConstraint.None;
						bool isDynamicPortList = false;
						bool isInput = false;

						if ( inputAttribute != null )
						{
							showBackingValue = inputAttribute.backingValue;
							connectionType = inputAttribute.connectionType;
							typeConstraint = inputAttribute.typeConstraint;
							isDynamicPortList = inputAttribute.dynamicPortList;
							isInput = true;
						}
						else if ( outputAttribute != null )
						{
							showBackingValue = outputAttribute.backingValue;
							connectionType = outputAttribute.connectionType;
							typeConstraint = outputAttribute.typeConstraint;
							isDynamicPortList = outputAttribute.dynamicPortList;
							isInput = false;
						}

						// The port didn't exist... let's just make it exist again?
						if ( port == null )
						{
							Node.UpdatePorts();
							port = Node.GetPort( info.PropertyName );
						}

						var portInfo = InspectorPropertyInfo.CreateValue(
							$"{info.PropertyName}:port",
							0,
							Property.ValueEntry.SerializationBackend,
							new GetterSetter<TValue, NodePort>(
								( ref TValue owner ) => port,
								( ref TValue owner, NodePort value ) => { }
							)
							, new HideInInspector()
						);

						var nodePortInfo = new NodePortInfo(
							portInfo,
							info,
							baseFieldName,
							info.TypeOfValue,
							Property.Tree.WeakTargets.FirstOrDefault() as Node, // Needed?
							showBackingValue,
							connectionType,
							typeConstraint,
							isDynamicPortList,
							false,
							isInput,
							true
						);

						propertyToNodeProperty[info.PropertyName] = portInfo.PropertyName;
						nameToNodePropertyInfo[portInfo.PropertyName] = nodePortInfo;

						if ( isDynamicPortList )
						{
							var listDrawerAttributes = info.GetAttribute<ListDrawerSettingsAttribute>();
							if ( listDrawerAttributes == null )
							{
								listDrawerAttributes = new ListDrawerSettingsAttribute();
								info.GetEditableAttributesList().Add( listDrawerAttributes );
							}

							listDrawerAttributes.Expanded = true;
							listDrawerAttributes.ShowPaging = false;
						}

						infos.Insert( i, portInfo );
						++i; // Skip the next entry
					}
				}

				if ( displayDynamicPortsAttribute != null )
				{
					// If I find any dynamic ports that were not covered here then add them as well
					// This should include anything that wouldn't be directly related to the ports I *did* find
					foreach ( var port in Node.Ports )
					{
						if ( port.IsDynamic )
						{
							// If is likely to be an automatically added port?
							if ( IsManagedPort( port.fieldName ) )
								continue;

							// No one claimed it?
							var nodePortInfo = CreateLooseDynamicPortInfo( port, out var info, out var portInfo, displayDynamicPortsAttribute );

							propertyToNodeProperty[info.PropertyName] = portInfo.PropertyName;
							nameToNodePropertyInfo[portInfo.PropertyName] = nodePortInfo;

							infos.Add( info );
							infos.Add( portInfo );
						}
					}
				}
			}

			for ( int i = 0; i < this.processors.Count; i++ )
			{
				ProcessedMemberPropertyResolverExtensions.ProcessingOwnerType = typeof( TValue );
				this.processors[i].ProcessMemberProperties( infos );
			}

			EditorApplication.update -= Update;
			if ( displayDynamicPortsAttribute != null )
				EditorApplication.update += Update;

			knownPortKeys.Clear();
			knownPortKeys.AddRange( Node.Ports.Select( x => x.fieldName ) );
			return InspectorPropertyInfoUtility.BuildPropertyGroupsAndFinalize( this.Property, typeof( TValue ), infos, includeSpeciallySerializedMembers );
		}

		protected void RemoveProperty( int index )
		{
			var info = infos[index];
			infos.RemoveAt( index );
			namesToIndex.Remove( info.PropertyName );

			var keys = new List<string>( namesToIndex.Keys ); // Pool?
			foreach ( var key in keys )
			{
				int value = namesToIndex[key];
				if ( value > index )
					namesToIndex[key] = value - 1;
			}
		}

		public virtual bool ChildPropertyRequiresRefresh( int index, InspectorPropertyInfo info )
		{
			return this.GetChildInfo( index ) != info;
		}

		public void RememberDynamicPort( InspectorProperty property )
		{
			var nodePortInfo = GetNodePortInfo( property.Name );
			if ( nodePortInfo.IsInput )
				nodePortInfo.Node.AddDynamicInput( nodePortInfo.Type, nodePortInfo.ConnectionType, nodePortInfo.TypeConstraint, nodePortInfo.BaseFieldName );
			else
				nodePortInfo.Node.AddDynamicOutput( nodePortInfo.Type, nodePortInfo.ConnectionType, nodePortInfo.TypeConstraint, nodePortInfo.BaseFieldName );
		}

		public void ForgetDynamicPort( InspectorProperty property )
		{
			var nodePortInfo = GetNodePortInfo( property.Name );

			propertyToNodeProperty.Remove( nodePortInfo.SourcePropertyInfo.PropertyName );
			nameToNodePropertyInfo.Remove( nodePortInfo.BaseFieldName );

			// Also remove it from the arrays - a bit of work since it could be nested?
			int infoIndex = namesToIndex[nodePortInfo.SourcePropertyInfo.PropertyName];
			RemoveProperty( infoIndex );

			int portInfoIndex = namesToIndex[nodePortInfo.PortPropertyInfo.PropertyName];
			RemoveProperty( portInfoIndex );

			// If the port still exists then kill it
			if ( nodePortInfo.Port != null && nodePortInfo.Node != null )
				nodePortInfo.Node.RemoveDynamicPort( nodePortInfo.Port );
		}

		public NodePortInfo CreateLooseDynamicPortInfo( NodePort port, out InspectorPropertyInfo info, out InspectorPropertyInfo portInfo, params Attribute[] attributes )
		{
			info = InspectorPropertyInfo.CreateValue(
				port.fieldName,
				1,
				Property.ValueEntry.SerializationBackend,
				new GetterSetter<TValue, int>(
					( ref TValue owner ) => 0,
					( ref TValue owner, int value ) => { }
				),
				attributes
			);

			portInfo = InspectorPropertyInfo.CreateValue(
				$"{info.PropertyName}:port",
				0,
				Property.ValueEntry.SerializationBackend,
				new GetterSetter<TValue, NodePort>(
					( ref TValue owner ) => port,
					( ref TValue owner, NodePort value ) => { }
				)
				, new HideInInspector()
			);

			// Create a fake property and a fake node
			NodePortInfo nodePortInfo = new NodePortInfo(
				portInfo,
				info,
				port.fieldName,
				port.ValueType,
				Property.Tree.WeakTargets.FirstOrDefault() as Node, // Needed?
				ShowBackingValue.Never,
				port.connectionType,
				port.typeConstraint,
				false,
				true,
				port.IsInput,
				false
			);

			return nodePortInfo;
		}

		protected List<string> knownPortKeys = new List<string>();
		protected List<string> currentPortKeys = new List<string>();

		protected bool IsManagedPort( string portName )
		{
			if ( GetNodePortInfo( portName ) != null )
				return true;

			// If is likely to be an automatically added port?
			var match = s_DynamicPortRegex.Match( portName );
			if ( match != null ) // Matched numbered ports
			{
				var basePortName = match.Groups[1].Value;
				var foundPort = Node.GetPort( basePortName );
				if ( foundPort != null && foundPort.IsStatic )
					return true;
			}

			return false;
		}

		// Listening for port changes
		protected void Update()
		{
			currentPortKeys.Clear();
			currentPortKeys.AddRange( Node.Ports.Select( x => x.fieldName ) );

			var lostPorts = knownPortKeys.Except( currentPortKeys );
			var gainedPorts = currentPortKeys.Except( knownPortKeys );

			foreach ( var lost in lostPorts )
			{
				if ( IsManagedPort( lost ) )
					continue;
			}
			foreach ( var gain in gainedPorts )
			{
				if ( IsManagedPort( gain ) )
					continue;

				// Check if we already know about it
				if ( nameToNodePropertyInfo.ContainsKey( gain ) )
					continue;

				var port = Node.GetPort( gain );

				// No one claimed it?
				var nodePortInfo = CreateLooseDynamicPortInfo( port, out var info, out var portInfo, displayDynamicPortsAttribute );

				propertyToNodeProperty[info.PropertyName] = portInfo.PropertyName;
				nameToNodePropertyInfo[portInfo.PropertyName] = nodePortInfo;

				namesToIndex[info.PropertyName] = this.infos.Count;
				this.infos.Add( info );
				namesToIndex[portInfo.PropertyName] = this.infos.Count;
				this.infos.Add( portInfo );
			}

			knownPortKeys.Clear();
			knownPortKeys.AddRange( currentPortKeys );
		}
	}

	public class DefaultNodePropertyPortResolver<T> : NodePropertyPortResolver<T>
		where T : Node
	{
	}
}
