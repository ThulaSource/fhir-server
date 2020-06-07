﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Utility;

namespace Microsoft.Health.Fhir.Core.Serialization.SourceNodes
{
    public abstract class BaseSourceNode<T> : ISourceNode, IResourceTypeSupplier, IAnnotated
        where T : IExtensionData
    {
        private IList<(string Name, Lazy<IEnumerable<ISourceNode>> Node)> _cachedNodes;

        protected BaseSourceNode(T resource)
        {
            Resource = resource;
        }

        public abstract string Name { get; }

        public abstract string Text { get; }

        public abstract string Location { get; }

        public abstract string ResourceType { get; }

        public T Resource { get; }

        public IEnumerable<object> Annotations(Type type)
        {
            if (type == GetType() || type == typeof(ISourceNode) || type == typeof(IResourceTypeSupplier))
            {
                return new[] { this };
            }

            return Enumerable.Empty<object>();
        }

        public IEnumerable<ISourceNode> Children(string name = null)
        {
            if (_cachedNodes == null)
            {
                _cachedNodes = PropertySourceNodes().Concat(ExtensionSourceNodes()).ToList();
            }

            if (name == null)
            {
                return _cachedNodes.SelectMany(x => x.Node.Value);
            }

            return _cachedNodes
                .Where(x => string.Equals(name, x.Name, StringComparison.Ordinal))
                .SelectMany(x => x.Node.Value);
        }

        private IEnumerable<(string Name, Lazy<IEnumerable<ISourceNode>> Node)> ExtensionSourceNodes()
        {
            var properties = Resource.ExtensionData ?? Enumerable.Empty<KeyValuePair<string, JsonElement>>();
            return JsonElementSourceNode.ProcessObjectProperties(properties.Select(x => (x.Key, x.Value)), Location);
        }

        protected abstract IEnumerable<(string Name, Lazy<IEnumerable<ISourceNode>> Node)> PropertySourceNodes();
    }
}
