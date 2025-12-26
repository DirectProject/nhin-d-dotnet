/* 
 Copyright (c) 2010, Direct Project
 All rights reserved.

 Authors:
    John Theisen
  
Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:

Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
Neither the name of The Direct Project (directproject.org) nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 
*/
using System;
using System.Linq;
using System.Web.Mvc;
using Health.Direct.Admin.Console.Models.Repositories;
using AutoMapper;
using Health.Direct.Admin.Console.Models.Pagination;

namespace Health.Direct.Admin.Console.Controllers
{
    public abstract class ControllerBase<T, TModel, TRepository, TEntityStatus> : ControllerErrorBase
        where T : class
        where TRepository : IRepository<T>
        where TEntityStatus : Enum
    {
        protected const int DefaultPageSize = 10;

        private static readonly Lazy<TEntityStatus> s_enabled =
            new Lazy<TEntityStatus>(() => FindRequiredValue("Enabled"));

        private static readonly Lazy<TEntityStatus> s_disabled =
            new Lazy<TEntityStatus>(() => FindRequiredValue("Disabled"));

        private readonly TRepository _repository;

        protected ControllerBase(TRepository repository)
        {
            if (repository == null)
            {
                throw new ArgumentNullException(nameof(repository));
            }
            _repository = repository;
        }

        protected TRepository Repository => _repository;

        protected abstract void SetStatus(T item, TEntityStatus status);

        private static TEntityStatus FindRequiredValue(string name)
        {
            foreach (var value in Enum.GetValues(typeof(TEntityStatus)))
            {
                if (string.Equals(value.ToString(), name, StringComparison.OrdinalIgnoreCase))
                {
                    return (TEntityStatus)value;
                }
            }
            throw new InvalidOperationException(
                $"Enum {typeof(TEntityStatus).FullName} must define a member named '{name}'.");
        }

        protected ActionResult IndexBase(int? page)
        {
            ViewData["DateTimeFormat"] = "M/d/yyyy h:mm:ss tt";

            var pageNumber = page.GetValueOrDefault(1);
            if (pageNumber < 1) pageNumber = 1;

            var query = Repository.Query();
            var totalCount = query.Count();

            var items = query
                .Skip((pageNumber - 1) * DefaultPageSize)
                .Take(DefaultPageSize)
                .Select(item => Mapper.Map<T, TModel>(item))
                .ToList();

            var paged = new PaginatedList<TModel>(items, pageNumber, DefaultPageSize, totalCount);
            return View(paged);
        }

        [Authorize]
        [HttpPost]
        public string Delete(long id)
        {
            try
            {
                var item = Repository.Get(id);
                if (item == null) return "NotFound";
                Repository.Delete(item);
                return bool.TrueString;
            }
            catch (Exception ex)
            {
                return ex.GetBaseException().Message;
            }
        }

        [Authorize]
        public ActionResult Enable(long id) => EnableDisable(id, s_enabled.Value);

        [Authorize]
        public ActionResult Disable(long id) => EnableDisable(id, s_disabled.Value);

        protected virtual ActionResult EnableDisable(long id, TEntityStatus status)
        {
            var item = Repository.Get(id);
            if (item == null) return View("NotFound");

            SetStatus(item, status);
            Repository.Update(item);

            return Json(Mapper.Map<T, TModel>(item), "text/json");
        }

        protected byte[] GetFileFromRequest(string keyName)
        {
            var file = Request.Files.Get(keyName);
            var bytes = new byte[file.ContentLength];
            file.InputStream.Read(bytes, 0, file.ContentLength);
            return bytes;
        }
    }

    // Optional: retain the 3-generic convenience base if many controllers use a common enum.
    // Uncomment and adjust the namespace + enum type if you later unify on one enum.
    /*
    public abstract class ControllerBase<T, TModel, TRepository>
        : ControllerBase<T, TModel, TRepository, Some.Shared.Namespace.EntityStatus>
        where T : class
        where TRepository : IRepository<T>
    {
        protected ControllerBase(TRepository repository) : base(repository) { }
    }
    */
}