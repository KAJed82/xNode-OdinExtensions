
using XNode;

using static XNode.Node;

namespace XNodeEditor.Odin
{
    public interface INodePropertyPortResolver
    {
        Node Node { get; }
        NodePort Port { get; }

        bool IsInput { get; }

        ShowBackingValue ShowBackingValue { get; }
        ConnectionType ConnectionType { get; }
        TypeConstraint TypeConstraint { get; }
        bool IsDynamicPortList { get; }
    }
}
