/*
 Copyright (c) 2010-2025, Direct Project
 All rights reserved.

 Authors:
    Joe Shook       Joseph.Shook@Surescripts.com

Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:

Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
Neither the name of The Direct Project (directproject.org) nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

*/

using System.Linq;
using System.Web.Mvc;
using AutoMapper;
using Health.Direct.Admin.Console.Models;
using Health.Direct.Admin.Console.Models.Repositories;
using Health.Direct.Admin.Console.Models.Pagination;
using Health.Direct.Config.Client.DomainManager;
using Health.Direct.Config.Client.MonitorService; // added

namespace Health.Direct.Admin.Console.Controllers
{
    public class MdnRecordsController : ControllerBase<Mdn, MdnModel, IMdnRecordRepository, EntityStatus>
    {
        public MdnRecordsController(IMdnRecordRepository repository)
            : base(repository)
        {
        }

        [Authorize]
        public ActionResult Index(int? page)
        {
            int pageNumber = page.GetValueOrDefault(1);
            if (pageNumber < 1) pageNumber = 1;

            var query = Repository.Query();

            int totalCount = query.Count();

            // Ensure deterministic paging; adjust OrderBy if Mdn has a different key
            var pageItems = query
                .OrderBy(m => m.Id)        // assumes Mdn has ID property
                .Skip((pageNumber - 1) * DefaultPageSize)
                .Take(DefaultPageSize)
                .Select(m => Mapper.Map<Mdn, MdnModel>(m))
                .ToList();

            var paged = new PaginatedList<MdnModel>(pageItems, pageNumber, DefaultPageSize, totalCount);

            return View(paged);
        }

        protected override void SetStatus(Mdn item, EntityStatus status)
        {
        }
    }
}