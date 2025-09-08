using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Health.Direct.Config.Client.DomainManager
{
    public partial class CertPolicyGroup
    {
        public CertPolicyGroup(string name, string description)
        {
            Name = name;
            Description = description;
        }
    }

    public partial class CertPolicy
    {
        public CertPolicy(string name, string description, byte[] data)
        {
            Name = name;
            Description = description;
            Lexicon = "SimpleText";
            Data = data;
        }
    }
}