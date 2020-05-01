using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;

using UnityEngine;

using XNode;

using static XNode.Node;

namespace XNodeEditor.Odin
{
	public interface INodePortResolver
	{
		Node Node { get; }

		NodePortInfo GetNodePortInfo( InspectorPropertyInfo sourceProperty );
	}

	public class NodePortInfo
	{
		private ConnectionType _connectionType;
		private TypeConstraint _typeConstraint;

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
	public abstract class NodePropertyPortResolver<T> : BaseMemberPropertyResolver<T>, IDisposable, INodePortResolver
	{
		protected static Regex s_DynamicPortRegex = new Regex( @"^(.+) (\d)$" );

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
		}

		public Node Node { get; private set; }

		protected Dictionary<InspectorPropertyInfo, NodePortInfo> propertyInfoToNodePropertyInfo = new Dictionary<InspectorPropertyInfo, NodePortInfo>();
		protected Dictionary<NodePort, NodePortInfo> nodePortToNodePortInfo = new Dictionary<NodePort, NodePortInfo>();

		protected override void Initialize()
		{
			base.Initialize();

			Node = Property.Tree.WeakTargets.FirstOrDefault() as Node;
		}

		protected override InspectorPropertyInfo[] GetPropertyInfos()
		{
			if ( this.processors == null )
			{
				this.processors = OdinPropertyProcessorLocator.GetMemberProcessors( this.Property );
			}

			var includeSpeciallySerializedMembers = !this.Property.ValueEntry.SerializationBackend.IsUnity;
			var infos = InspectorPropertyInfoUtility.CreateMemberProperties( this.Property, typeof( T ), includeSpeciallySerializedMembers );

			// If we resolve the ports from the port dictionary i might be able to communicate between properties
			// in order to make dynamic port adding cleaner

			// Port makers
			{
				for ( int i = 0; i < infos.Count; ++i )
				{
					var info = infos[i];
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

						var nodePortInfo = new NodePortInfo(
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

						propertyInfoToNodePropertyInfo[info] = nodePortInfo;
						nodePortToNodePortInfo[port] = nodePortInfo;

						var portInfo = InspectorPropertyInfo.CreateValue(
							$"{info.PropertyName}:port",
							0,
							Property.ValueEntry.SerializationBackend,
							new GetterSetter<T, NodePort>(
								( ref T owner ) => nodePortInfo.Port,
								( ref T owner, NodePort value ) => { }
							)
							, new HideInInspector()
						);

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

				// If I find any dynamic ports that were not covered here then add them as well
				// This should include anything that wouldn't be directly related to the ports I *did* find
				foreach ( var port in Node.Ports )
				{
					if ( port.IsDynamic )
					{
						// If is likely to be an automatically added port?
						var match = s_DynamicPortRegex.Match( port.fieldName );
						if ( match != null ) // Matched numbered ports
						{
							var basePortName = match.Groups[1].Value;
							var foundPort = Node.GetPort( basePortName );
							if ( foundPort != null && foundPort.IsStatic )
								continue;
						}

						var info = InspectorPropertyInfo.CreateValue(
							port.fieldName,
							1,
							Property.ValueEntry.SerializationBackend,
							new GetterSetter<T, int>(
								( ref T owner ) => 0,
								( ref T owner, int value ) => { }
							)
						);

						// Create a fake property and a fake node
						var nodePortInfo = new NodePortInfo(
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

						propertyInfoToNodePropertyInfo[info] = nodePortInfo;
						nodePortToNodePortInfo[port] = nodePortInfo;

						var portInfo = InspectorPropertyInfo.CreateValue(
							$"{info.PropertyName}:port",
							0,
							Property.ValueEntry.SerializationBackend,
							new GetterSetter<T, NodePort>(
								( ref T owner ) => nodePortInfo.Port,
								( ref T owner, NodePort value ) => { }
							)
							, new HideInInspector()
						);

						// No one claimed it?

						infos.Add( info );
						infos.Add( portInfo );
					}
				}
			}

			for ( int i = 0; i < this.processors.Count; i++ )
			{
				ProcessedMemberPropertyResolverExtensions.ProcessingOwnerType = typeof( T );
				this.processors[i].ProcessMemberProperties( infos );
			}
			
			return InspectorPropertyInfoUtility.BuildPropertyGroupsAndFinalize( this.Property, typeof( T ), infos, includeSpeciallySerializedMembers );
		}

		public NodePortInfo GetNodePortInfo( NodePort port )
		{
			NodePortInfo nodePortInfo;
			nodePortToNodePortInfo.TryGetValue( port, out nodePortInfo );
			return nodePortInfo;
		}

		public NodePortInfo GetNodePortInfo( InspectorPropertyInfo sourceProperty )
		{
			NodePortInfo nodePortInfo;
			propertyInfoToNodePropertyInfo.TryGetValue( sourceProperty, out nodePortInfo );
			return nodePortInfo;
		}
	}

	public class DefaultNodePropertyPortResolver<T> : NodePropertyPortResolver<T>
		where T : Node
	{
	}
}
