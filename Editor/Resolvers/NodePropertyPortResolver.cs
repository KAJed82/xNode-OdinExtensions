
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector.Editor.TypeSearch;
using Sirenix.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using UnityEditor;
using UnityEngine;
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

    public interface ISimpleNodePropertyPortResolver : INodePropertyPortResolver { }

    // This only works for simple types that wouldn't normally have children
    [ResolverPriority( 1000000 )]
    public class NodePropertyPortResolver<T> : OdinPropertyResolver<T>, ISimpleNodePropertyPortResolver
    {
        #region Resolver Helpers
        private static TypeSearchIndex s_ResolverSearchIndex;
        public static TypeSearchIndex ResolverSearchIndex
        {
            get
            {
                if ( s_ResolverSearchIndex == null )
                {
                    var searchIndexField = typeof( DefaultOdinPropertyResolverLocator )
                        .FindMember()
                        .IsStatic()
                        .HasReturnType<TypeSearchIndex>()
                        .IsNamed( "SearchIndex" )
                        .GetMember<FieldInfo>();

                    s_ResolverSearchIndex = searchIndexField.GetValue( null ) as TypeSearchIndex;
                }
                return s_ResolverSearchIndex;
            }
        }
        private static Dictionary<Type, OdinPropertyResolver> resolverEmptyInstanceMap = new Dictionary<Type, OdinPropertyResolver>( FastTypeComparer.Instance );
        private static readonly List<TypeSearchResult[]> QueryResultsList = new List<TypeSearchResult[]>();
        private static readonly List<TypeSearchResult> MergedSearchResultsList = new List<TypeSearchResult>();
        private OdinPropertyResolver GetEmptyResolverInstance( Type resolverType )
        {
            OdinPropertyResolver result;
            if ( !resolverEmptyInstanceMap.TryGetValue( resolverType, out result ) )
            {
                result = (OdinPropertyResolver)FormatterServices.GetUninitializedObject( resolverType );
                resolverEmptyInstanceMap[resolverType] = result;
            }
            return result;
        }

        /// <summary>
        /// Gets an <see cref="OdinPropertyResolver"/> instance for the specified property.
        /// </summary>
        /// <param name="property">The property to get an <see cref="OdinPropertyResolver"/> instance for.</param>
        /// <returns>An instance of <see cref="OdinPropertyResolver"/> to resolver the specified property.</returns>
        public OdinPropertyResolver GetNextResolver( InspectorProperty property, OdinPropertyResolver ignoredResolver )
        {
            if ( property.Tree.IsStatic && property == property.Tree.SecretRootProperty )
            {
                return OdinPropertyResolver.Create( typeof( StaticRootPropertyResolver<> ).MakeGenericType( property.ValueEntry.TypeOfValue ), property );
            }

            var queries = QueryResultsList;
            queries.Clear();

            queries.Add( ResolverSearchIndex.GetMatches( Type.EmptyTypes ) );

            Type typeOfValue = property.ValueEntry != null ? property.ValueEntry.TypeOfValue : null;

            if ( typeOfValue != null )
            {
                queries.Add( ResolverSearchIndex.GetMatches( typeOfValue ) );

                for ( int i = 0; i < property.Attributes.Count; i++ )
                {
                    queries.Add( ResolverSearchIndex.GetMatches( typeOfValue, property.Attributes[i].GetType() ) );
                }
            }

            TypeSearchIndex.MergeQueryResultsIntoList( queries, MergedSearchResultsList );

            for ( int i = 0; i < MergedSearchResultsList.Count; i++ )
            {
                var info = MergedSearchResultsList[i];
                if ( info.MatchedType == ignoredResolver.GetType() )
                    continue;

                if ( GetEmptyResolverInstance( info.MatchedType ).CanResolveForPropertyFilter( property ) )
                {
                    return OdinPropertyResolver.Create( info.MatchedType, property );
                }
            }

            return OdinPropertyResolver.Create<EmptyPropertyResolver>( property );
        }
        #endregion

        public override bool CanResolveForPropertyFilter( InspectorProperty property )
        {
            if ( !NodeEditor.InNodeEditor )
                return false;

            //if ( property.ParentValueProperty == null && property.ParentType != null && property.ParentType.ImplementsOrInherits( typeof( Node ) ) ) // Base fields
            if ( property.Info.GetMemberInfo() != null && property.Info.GetMemberInfo().DeclaringType.ImplementsOrInherits( typeof( Node ) ) ) // It's at least a member!
            {
                var inputAttribute = property.GetAttribute<InputAttribute>();
                if ( inputAttribute != null )
                    return !inputAttribute.dynamicPortList;

                var outputAttribute = property.GetAttribute<OutputAttribute>();
                if ( outputAttribute != null )
                    return !outputAttribute.dynamicPortList;

                return false;
            }

            // Resolved by one of the dynamic port list resolvers
            if ( property.ParentValueProperty != null && property.ParentValueProperty.ChildResolver is IDynamicPortListNodePropertyResolverWithPorts )
                return true;

            return false;
        }

        protected override bool AllowNullValues => true;

        protected OdinPropertyResolver<T> backupResolver;

        public Node Node { get; private set; }
        public NodePort Port { get; private set; }

        public ShowBackingValue ShowBackingValue { get; private set; }
        public ConnectionType ConnectionType { get; private set; }
        public TypeConstraint TypeConstraint { get; private set; }
        public bool IsDynamicPortList { get; private set; }

        public bool IsInput { get; private set; }

        public IDynamicPortListNodePropertyResolverWithPorts ParentNoDataResolver { get; private set; }

        public InspectorPropertyInfo PortInfo { get; private set; }

        protected override void Initialize()
        {
            Node = Property.Tree.WeakTargets.FirstOrDefault() as Node;
            if ( Property.ParentValueProperty != null && Property.ParentValueProperty.ChildResolver is IDynamicPortListNodePropertyResolverWithPorts )
            {
                ParentNoDataResolver = Property.ParentValueProperty.ChildResolver as IDynamicPortListNodePropertyResolverWithPorts;
                var fieldName = $"{ParentNoDataResolver.FieldName} {Property.Index}";
                Port = Node.GetPort( fieldName );

                ShowBackingValue = ParentNoDataResolver.ShowBackingValue;
                ConnectionType = ParentNoDataResolver.ConnectionType;
                TypeConstraint = ParentNoDataResolver.TypeConstraint;
                IsDynamicPortList = false;
                IsInput = ParentNoDataResolver.IsInput;

                // We expected to find a port but didn't let's fix it
                if ( Port == null )
                {
                    if ( IsInput )
                        Port = Node.AddDynamicInput( typeof( T ), ConnectionType, TypeConstraint, fieldName );
                    else
                        Port = Node.AddDynamicOutput( typeof( T ), ConnectionType, TypeConstraint, fieldName );
                }
            }
            else
            {
                Port = Node.GetPort( Property.Name );

                var inputAttribute = Property.GetAttribute<InputAttribute>();
                var outputAttribute = Property.GetAttribute<OutputAttribute>();
                if ( inputAttribute != null )
                {
                    ShowBackingValue = inputAttribute.backingValue;
                    ConnectionType = inputAttribute.connectionType;
                    TypeConstraint = inputAttribute.typeConstraint;
                    IsDynamicPortList = inputAttribute.dynamicPortList;
                    IsInput = true;
                }
                else if ( outputAttribute != null )
                {
                    ShowBackingValue = outputAttribute.backingValue;
                    ConnectionType = outputAttribute.connectionType;
                    TypeConstraint = outputAttribute.typeConstraint;
                    IsDynamicPortList = outputAttribute.dynamicPortList;
                    IsInput = false;
                }
            }

            PortInfo = InspectorPropertyInfo.CreateValue(
              NodePropertyPort.NodePortPropertyName,
              0,
              Property.ValueEntry.SerializationBackend,
              new GetterSetter<T, NodePort>(
                ( ref T owner ) => Port,
                ( ref T owner, NodePort value ) => { }
              )
            );

            // Grab the next correct resolver and let it do it's thing
            backupResolver = GetNextResolver( Property, this ) as OdinPropertyResolver<T>;
        }

        public override int ChildNameToIndex( string name )
        {
            if ( backupResolver == null )
                return 1;

            if ( name == NodePropertyPort.NodePortPropertyName )
                return backupResolver.ChildCount;

            return backupResolver.ChildNameToIndex( name );
        }

        private InspectorPropertyInfo info;

        public override InspectorPropertyInfo GetChildInfo( int childIndex )
        {
            if ( backupResolver == null )
                return PortInfo;

            if ( childIndex == backupResolver.ChildCount )
                return PortInfo;

            return backupResolver.GetChildInfo( childIndex );
        }

        protected override int GetChildCount( T value )
        {
            if ( backupResolver == null )
                return 1;

            return backupResolver.ChildCount + 1;
        }
    }

}
