<%@ Page Title="" Language="C#" MasterPageFile="~/Views/Shared/Site.Master" Inherits="System.Web.Mvc.ViewPage<IEnumerable<DnsRecordModel>>" %>
<%@ Import Namespace="Health.Direct.Admin.Console.Models"%>
<%@ Import Namespace="Health.Direct.Admin.Console.Controllers"%>

<asp:Content ID="Content1" ContentPlaceHolderID="TitleContent" runat="server">
	DNS Records
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">

    <%= Html.Partial(ViewData["Domain"] == null ? "AllItemsReminder" : "FilterReminder", "dnsrecords")%>
    <div class="action-bar clear">
        <%= Html.ActionLink("Add A Record", "AddAname", null, new { @class = "action ui-priority-primary" })%>
        <%= Html.ActionLink("Add MX Record", "AddMx", null, new { @class = "action ui-priority-primary" })%>
        <%= Html.ActionLink("Add SOA Record", "AddSoa", null, new { @class = "action ui-priority-primary" })%>
    </div>

    <%= Html.Partial("DnsRecordList", Model, ViewData) %>
    <%= Html.Partial("DnsRecordDetailsDialog") %>

</asp:Content>
