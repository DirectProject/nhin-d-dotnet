using System.Linq;
using System.Web.Mvc;
using AutoMapper;
using Health.Direct.Admin.Console.Models;
using Health.Direct.Admin.Console.Models.Repositories;
using Health.Direct.Admin.Console.Models.Pagination;
using Health.Direct.Config.Client.DomainManager; // added

namespace Health.Direct.Admin.Console.Controllers
{
    public class DnsRecordsController : ControllerBase<DnsRecord, DnsRecordModel, IDnsRecordRepository, EntityStatus>
    {
        public DnsRecordsController(IDnsRecordRepository repository) : base(repository)
        {
        }

        [Authorize]
        public ActionResult Index(int? page)
        {
            int pageNumber = page.GetValueOrDefault(1);
            if (pageNumber < 1) pageNumber = 1;

            var query = Repository.Query();

            int totalCount = query.Count();

            // Ensure deterministic ordering for paging (adjust if another field preferred)
            var pageItems = query
                .OrderBy(r => r.ID)
                .Skip((pageNumber - 1) * DefaultPageSize)
                .Take(DefaultPageSize)
                .Select(record => Mapper.Map<DnsRecord, DnsRecordModel>(record))
                .ToList();

            var paged = new PaginatedList<DnsRecordModel>(pageItems, pageNumber, DefaultPageSize, totalCount);

            return View(paged);
        }

        [Authorize]
        public ActionResult AddMx()
        {
            return View(new MxRecordModel());
        }

        [Authorize]
        [HttpPost]
        public ActionResult AddMx(FormCollection formValues)
        {
            return Add<MxRecordModel>();
        }

        [Authorize]
        public ActionResult AddAname()
        {
            return View(new AddressRecordModel());
        }

        [Authorize]
        [HttpPost]
        public ActionResult AddAname(FormCollection formValues)
        {
            return Add<AddressRecordModel>();
        }

        [Authorize]
        public ActionResult AddSoa()
        {
            return View(new SoaRecordModel());
        }

        [Authorize]
        [HttpPost]
        public ActionResult AddSoa(FormCollection formValues)
        {
            return Add<SoaRecordModel>();
        }

        private ActionResult Add<T>()
            where T : DnsRecordModel, new()
        {
            var model = new T();

            if (TryUpdateModel(model))
            {
                Repository.Add(Mapper.Map<T, DnsRecord>(model));
                return RedirectToAction("Index");
            }

            return View(model);
        }

        [Authorize]
        public ActionResult AnameDetails(long id) => Details<AddressRecordModel>(id);

        [Authorize]
        public ActionResult MxDetails(long id) => Details<MxRecordModel>(id);

        [Authorize]
        public ActionResult SoaDetails(long id) => Details<SoaRecordModel>(id);

        private ActionResult Details<T>(long id)
            where T : DnsRecordModel, new()
        {
            var dnsRecord = Repository.Get(id);
            if (dnsRecord == null) return View("NotFound");

            var recordModel = new T();
            Mapper.Map(dnsRecord, recordModel, typeof(DnsRecord), typeof(DnsRecordModel));
            return PartialView(recordModel);
        }

        [Authorize]
        public ActionResult EditAname(long id) => Edit<AddressRecordModel>(id);

        [Authorize]
        public ActionResult EditMx(long id) => Edit<MxRecordModel>(id);

        [Authorize]
        public ActionResult EditSoa(long id) => Edit<SoaRecordModel>(id);

        private ActionResult Edit<T>(long id)
            where T : DnsRecordModel, new()
        {
            var dnsRecord = Repository.Get(id);
            if (dnsRecord == null) return View("NotFound");

            var recordModel = new T();
            Mapper.Map(dnsRecord, recordModel, typeof(DnsRecord), typeof(DnsRecordModel));
            return PartialView(recordModel);
        }

        [Authorize]
        [HttpPost]
        public ActionResult EditAname(FormCollection formValues) => Edit<AddressRecordModel>(formValues);

        [Authorize]
        [HttpPost]
        public ActionResult EditMx(FormCollection formValues) => Edit<MxRecordModel>(formValues);

        [Authorize]
        [HttpPost]
        public ActionResult EditSoa(FormCollection formValues) => Edit<SoaRecordModel>(formValues);

        private ActionResult Edit<T>(FormCollection formValues)
            where T : DnsRecordModel, new()
        {
            long id;
            long.TryParse(formValues["id"], out id);

            var dnsRecord = Repository.Get(id);
            if (dnsRecord == null) return View("NotFound");

            var recordModel = new T();
            Mapper.Map(dnsRecord, recordModel, typeof(DnsRecord), typeof(DnsRecordModel));

            if (TryUpdateModel(recordModel))
            {
                Mapper.Map(recordModel, dnsRecord, typeof(DnsRecordModel), typeof(DnsRecord));
                Repository.Update(dnsRecord);
                return RedirectToAction("Index");
            }

            return View("Edit" + recordModel.TypeString, recordModel);
        }

        protected override void SetStatus(DnsRecord item, EntityStatus status)
        {
        }
    }
}