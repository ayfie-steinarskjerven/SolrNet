#region license
// Copyright (c) 2007-2010 Mauricio Scheffer
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
//  
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace SolrNet.Impl {
    /// <summary>
    /// Serializes a Solr document to xml
    /// </summary>
    /// <typeparam name="T">Document type</typeparam>
    public class SolrDocumentSerializer<T> : ISolrDocumentSerializer<T> {
        private readonly IReadOnlyMappingManager mappingManager;
        private readonly ISolrFieldSerializer fieldSerializer;

        /// <inheritdoc/>
        public SolrDocumentSerializer(IReadOnlyMappingManager mappingManager, ISolrFieldSerializer fieldSerializer) {
            this.mappingManager = mappingManager;
            this.fieldSerializer = fieldSerializer;
        }

        private static readonly Regex ControlCharacters =
            new Regex(@"[^\x09\x0A\x0D\x20-\uD7FF\uE000-\uFFFD\u10000-u10FFFF]", RegexOptions.Compiled);

        /// <inheritdoc/>
        // http://stackoverflow.com/a/14323524/21239
        public static string RemoveControlCharacters(string xml) {
            if (xml == null)
                return null;
            return ControlCharacters.Replace(xml, "");
        }

        /// <inheritdoc/>
        public XElement Serialize(T doc, double? boost)
        {
            var docNode = new XElement("doc");
            if (boost.HasValue)
            {
                var boostAttr = new XAttribute("boost", boost.Value.ToString(CultureInfo.InvariantCulture));
                docNode.Add(boostAttr);
            }

            var fields = mappingManager.GetFields(doc.GetType());
            foreach (var field in fields.Values)
            {
                var p = field.Property;
                if (!p.CanRead)
                    continue;

                var value = p.GetValue(doc, null);
                if (value == null)
                    continue;

                if (value is IDictionary<string, object> dict)
                {
                    SerializeDictionary(dict, docNode);
                }
                else
                {
                    SerializeField(field, value, docNode);
                }
            }

            var documents = mappingManager.GetDocuments(doc.GetType());
            foreach (var document in documents)
            {
                var value = document.Value.GetValue(doc, null);
                if (value != null)
                {
                    if (typeof(IEnumerable).IsAssignableFrom(document.Value.PropertyType) && document.Value.PropertyType != typeof(string))
                    {
                        SerializeNestedDocumentCollection(value as IEnumerable, docNode);
                    }
                    else
                    {
                        SerializeNestedDocument(value, docNode);
                    }
                }
            }

            return docNode;
        }

        private void SerializeDictionary(IDictionary<string, object> dict, XElement parentNode)
        {
            foreach (var kvp in dict)
            {
                if (kvp.Value is IEnumerable enumerable && !(kvp.Value is string))
                {
                    foreach (var item in enumerable)
                    {
                        SerializeField(new SolrFieldModel(null, kvp.Key, null), item, parentNode);
                    }
                }
                else
                {
                    SerializeField(new SolrFieldModel(null, kvp.Key, null), kvp.Value, parentNode);
                }
            }
        }

        private void SerializeNestedDocumentCollection(IEnumerable collection, XElement parentNode)
        {
            if (collection == null) return;

            foreach (var item in collection)
            {
                SerializeNestedDocument(item, parentNode);
            }
        }

        private void SerializeNestedDocument(object nestedDoc, XElement parentNode)
        {
            var nestedDocNode = new XElement("doc");
            var nestedFields = mappingManager.GetFields(nestedDoc.GetType());
            foreach (var nestedField in nestedFields.Values)
            {
                var nestedValue = nestedField.Property.GetValue(nestedDoc, null);
                if (nestedValue == null)
                    continue;

                SerializeField(nestedField, nestedValue, nestedDocNode);
            }
            parentNode.Add(nestedDocNode);
        }

        private void SerializeField(SolrFieldModel field, object value, XElement parentNode)
        {
            var nodes = fieldSerializer.Serialize(value);
            foreach (var n in nodes)
            {
                var fieldName = field.FieldName == "*" ? n.FieldNameSuffix : field.FieldName + n.FieldNameSuffix;
                var fieldNode = new XElement("field", new XAttribute("name", fieldName));

                if (field.Boost.HasValue && field.Boost.Value > 0)
                {
                    var boostAtt = new XAttribute("boost", field.Boost.Value.ToString(CultureInfo.InvariantCulture));
                    fieldNode.Add(boostAtt);
                }

                var v = RemoveControlCharacters(n.FieldValue);
                if (v != null)
                {
                    fieldNode.Value = v;
                    parentNode.Add(fieldNode);
                }
            }
        }
    }
}
