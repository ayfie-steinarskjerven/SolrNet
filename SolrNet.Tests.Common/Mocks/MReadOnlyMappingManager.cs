using System;
using System.Collections.Generic;
using System.Reflection;

using Moroco;

namespace SolrNet.Tests.Mocks {
    public class MReadOnlyMappingManager : IReadOnlyMappingManager {
        public MFunc<Type, IDictionary<string, SolrFieldModel>> getFields;
        public MFunc<Type, SolrFieldModel> getUniqueKey;
        public MFunc<ICollection<Type>> getRegisteredTypes;

        public IDictionary<string, SolrFieldModel> GetFields(Type type) {
            return getFields.Invoke(type);
        }

        public SolrFieldModel GetUniqueKey(Type type) {
            return getUniqueKey.Invoke(type);
        }

        public ICollection<Type> GetRegisteredTypes() {
            return getRegisteredTypes.Invoke();
        }

        IDictionary<string, PropertyInfo> IReadOnlyMappingManager.GetDocuments(Type type)
        {
            throw new NotImplementedException();
        }
    }
}
