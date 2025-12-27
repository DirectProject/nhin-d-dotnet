<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl<IEnumerable<Health.Direct.Admin.Console.Models.MdnModel>>" %>
<%@ Import Namespace="Health.Direct.Admin.Console.Common" %>

<table class="grid ui-widget ui-widget-content">
    <thead class="ui-widget-header">
        <tr>
            <th style="display:none;">ID</th>
            <th>MDN Identifier</th>
            <th>Message ID</th>
            <th>Subject</th>
            <th>Sender</th>
            <th>Recipient</th>
            <th>Status</th>
            <th>Notify Dispatched</th>
            <th>Timed Out</th>
            <th>Processed On</th>
            <th>Created On</th>
            <th>Updated On</th>
        </tr>
    </thead>
    <tbody>
        <% foreach (var d in Model) { %>
            <tr>
                <td style="display:none;"><%= d.Id %></td>
                <td><%= d.MdnIdentifier %></td>
                <td><%= d.MessageId %></td>
                <td><%= d.SubjectValue %></td>
                <td><%= d.Sender %></td>
                <td><%= d.Recipient %></td>
                <td class="status"><%= d.Status %></td>
                <td><%= d.NotifyDispatched %></td>
                <td><%= d.Timedout %></td>
                <td><%= d.MdnProcessedDate %></td>
                <td><%= d.CreateDate %></td>
                <td><%= d.UpdateDate %></td>
            </tr>
        <% } %>
    </tbody>
</table>

<% var paged = Model as Health.Direct.Admin.Console.Models.Pagination.PaginatedList<Health.Direct.Admin.Console.Models.MdnModel>; %>
<% if (paged != null) { %>
<div class="pager">
    <% if (paged.HasPreviousPage) { %>
        <%= Html.ActionLink("Prev", ViewContext.RouteData.Values["action"].ToString(), new { page = paged.PageNumber - 1 }) %>
    <% } %>
    <span>Page <%= paged.PageNumber %> of <%= paged.PageCount %></span>
    <% if (paged.HasNextPage) { %>
        <%= Html.ActionLink("Next", ViewContext.RouteData.Values["action"].ToString(), new { page = paged.PageNumber + 1 }) %>
    <% } %>
</div>
<% } %>