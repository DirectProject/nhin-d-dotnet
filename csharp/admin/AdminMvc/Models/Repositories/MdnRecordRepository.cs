using System.Linq;
using Health.Direct.Config.Client.MonitorService;

namespace Health.Direct.Admin.Console.Models.Repositories
{
    public class MdnRecordRepository : IMdnRecordRepository
    {
        private readonly IMdnMonitor m_client;

		public MdnRecordRepository(IMdnMonitor client)
        {
            m_client = client;
        }

		protected IMdnMonitor Client { get { return m_client; } }

	    public Mdn Get(long id)
	    {
		    throw new System.NotImplementedException();
	    }

	    public Mdn Add(Mdn obj)
	    {
		    throw new System.NotImplementedException();
	    }

	    public Mdn Update(Mdn obj)
	    {
		    throw new System.NotImplementedException();
	    }

	    public void Delete(Mdn obj)
	    {
		    throw new System.NotImplementedException();
	    }

		public IQueryable<Mdn> Query()
        {
			return Client.EnumerateMdns(null, int.MaxValue).AsQueryable();
        }
    }
}