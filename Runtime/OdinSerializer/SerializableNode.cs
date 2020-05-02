
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace XNode.Odin
{
	[ShowOdinSerializedPropertiesInInspector]
	public abstract class SerializableNode : Node, ISerializationCallbackReceiver
	{
		#region Odin serialized data
		[SerializeField, HideInInspector]
		private SerializationData serializationData;

		void ISerializationCallbackReceiver.OnAfterDeserialize()
		{
			UnitySerializationUtility.DeserializeUnityObject( this, ref this.serializationData );
			this.OnAfterDeserialize();
		}

		void ISerializationCallbackReceiver.OnBeforeSerialize()
		{
			this.OnBeforeSerialize();
			UnitySerializationUtility.SerializeUnityObject( this, ref this.serializationData );
		}
		#endregion

		public virtual void OnAfterDeserialize()
		{
		}

		public virtual void OnBeforeSerialize()
		{
		}
	}
}
