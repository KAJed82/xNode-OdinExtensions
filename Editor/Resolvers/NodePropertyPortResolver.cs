﻿using System;
using System.Collections.Generic;
using System.Linq;

using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;

using UnityEngine;

using XNode;

using static XNode.Node;

namespace XNodeEditor.Odin
{
	public interface INodePortResolver
	{
		NodePortInfo GetNodePortInfo( NodePort port );
		NodePortInfo GetNodePortInfo( InspectorPropertyInfo sourceProperty );
	}

	public class NodePortInfo
	{
		public InspectorPropertyInfo SourcePropertyInfo { get; private set; }

		public string BaseFieldName { get; private set; }

		public Node Node { get; private set; }
		public NodePort Port => Node.GetPort( BaseFieldName );

		public ShowBackingValue ShowBackingValue { get; private set; }
		public ConnectionType ConnectionType => Port.connectionType;
		public TypeConstraint TypeConstraint => Port.typeConstraint;
		public bool IsDynamicPortList { get; private set; }

		public bool IsInput { get; private set; }

		public NodePortInfo(
			InspectorPropertyInfo sourcePropertyInfo,
			string baseFieldName,
			Node node, // Needed?
			ShowBackingValue showBackingValue,
			bool isDynamicPortList,
			bool isInput
		)
		{
			SourcePropertyInfo = sourcePropertyInfo;
			BaseFieldName = baseFieldName;
			Node = node;
			ShowBackingValue = showBackingValue;
			IsDynamicPortList = isDynamicPortList;
			IsInput = isInput;
		}
	}

	// Invert the pattern
	// Inject this property into a node port holder
	[ResolverPriority( 10 )]
	public abstract class NodePropertyPortResolver<T> : BaseMemberPropertyResolver<T>, IDisposable, INodePortResolver
	{
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

		protected Dictionary<InspectorPropertyInfo, NodePortInfo> propertyInfoToNodePropertyInfo = new Dictionary<InspectorPropertyInfo, NodePortInfo>();
		protected Dictionary<NodePort, NodePortInfo> nodePortToNodePortInfo = new Dictionary<NodePort, NodePortInfo>();

		protected override InspectorPropertyInfo[] GetPropertyInfos()
		{
			var node = Property.Tree.WeakTargets.FirstOrDefault() as Node;

			if ( this.processors == null )
			{
				this.processors = OdinPropertyProcessorLocator.GetMemberProcessors( this.Property );
			}

			var includeSpeciallySerializedMembers = !this.Property.ValueEntry.SerializationBackend.IsUnity;
			var infos = InspectorPropertyInfoUtility.CreateMemberProperties( this.Property, typeof( T ), includeSpeciallySerializedMembers );

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
						NodePort port = node.GetPort( info.PropertyName );
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

						var nodePortInfo = new NodePortInfo(
							info,
							baseFieldName,
							Property.Tree.WeakTargets.FirstOrDefault() as Node, // Needed?
							showBackingValue,
							isDynamicPortList,
							isInput
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


						infos.Insert( i, portInfo );
						++i; // Skip the next entry
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
