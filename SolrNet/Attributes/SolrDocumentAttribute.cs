using System;

namespace SolrNet.Attributes {
    /// <summary>
    /// Marks a property as present on Solr. 
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class SolrDocumentAttribute : Attribute {
        /// <summary>
        /// Marks a property as present on Solr.
        /// </summary>
        public SolrDocumentAttribute() {}
    }
}