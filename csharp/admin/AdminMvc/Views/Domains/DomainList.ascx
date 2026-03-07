<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl<IEnumerable<DomainModel>>" %>
<%@ Import Namespace="Health.Direct.Admin.Console.Common"%>
<%@ Import Namespace="Health.Direct.Admin.Console.Models"%>

<table class="grid ui-widget ui-widget-content">
    <thead class="ui-widget-header">
        <tr>
            <th style="display:none;">ID</th>
            <th>Name</th>
            <th>Status</th>
            <th>Security Standard</th>
            <th>Created On</th>
            <th>Updated On</th>
            <th>Actions</th>
        </tr>
    </thead>
    <tbody>
        <% foreach (var d in Model) { %>
            <tr>
                <td style="display:none;"><%= d.ID %></td>
                <td><%= d.Name %></td>
                <td class="status"><%= d.Status %></td>
                <td><%= d.SecurityStandard %></td>
                <td><%= Html.Span(Formatter.Format(d.CreateDate), new { title = d.CreateDate }) %></td>
                <td><%= Html.Span(Formatter.Format(d.UpdateDate), new { @class = "update-date", title = d.UpdateDate }) %></td>
                <td>
                    <%= Html.ActionLink("View", "Details", new { id = d.ID }, new { @class = "view-details" }) %>
                    |
                    <%= Html.ActionLink("Addresses", "Addresses", new { id = d.ID }) %>
                    |
                    <%= Html.ActionLink("Anchors", "Anchors", new { id = d.ID }) %>
                    |
                    <%= Html.ActionLink("Certificates", "Certificates", new { id = d.ID }) %>
                    |
                    <% if (d.IsEnabled) { %>
                        <%= Html.ActionLink("Disable", "Disable", new { id = d.ID }, new { @class = "enable-disable-action" }) %>
                    <% } else { %>
                        <%= Html.ActionLink("Enable", "Enable", new { id = d.ID }, new { @class = "enable-disable-action" }) %>
                    <% } %>
                    |
                    <%= Html.ActionLink("Delete", "Delete", new { id = d.ID }, new { @class = "toolbar-button delete-action" }) %>
                </td>
            </tr>
        <% } %>
    </tbody>
</table>

<% var paged = Model as Health.Direct.Admin.Console.Models.Pagination.PaginatedList<DomainModel>; %>
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

<div id="confirm-dialog" style="display: none;"></div>

<script type="text/javascript" language="javascript">
    $(function() {
        $('a.delete-action')
            .button({ icons: { primary: "ui-icon-trash" }, text: false })
            .click(function(event) { confirmDelete(event, $('#confirm-dialog'), $(this), 'Are you sure want to delete this domain?', 'Domain') });

        $('a.view-details').click(function(event) {
            showDetailsDialog($('#domain-dialog'), event, $(this), 'Domain Details');
        });
    });
</script>
