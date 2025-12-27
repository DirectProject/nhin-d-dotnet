<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl<IEnumerable<AddressModel>>" %>
<%@ Import Namespace="Health.Direct.Admin.Console.Common"%>
<%@ Import Namespace="Health.Direct.Admin.Console.Models"%>

<table class="grid ui-widget ui-widget-content">
    <thead class="ui-widget-header">
        <tr>
            <th>Domain ID</th>
            <th>Email Address</th>
            <th>Display Name</th>
            <th>Status</th>
            <th>Created On</th>
            <th>Updated On</th>
            <th>Actions</th>
        </tr>
    </thead>
    <tbody>
        <% foreach (var a in Model) { %>
            <tr>
                <td><%= a.DomainID %></td>
                <td><%= a.EmailAddress %></td>
                <td><%= a.DisplayName %></td>
                <td class="status"><%= a.Status %></td>
                <td><%= Html.Span(Formatter.Format(a.CreateDate), new { title = a.CreateDate }) %></td>
                <td><%= Html.Span(Formatter.Format(a.UpdateDate), new { @class = "update-date", title = a.UpdateDate }) %></td>
                <td>
                    <%= Html.ActionLink("View", "Details", new { id = a.ID }, new { @class = "view-details" }) %> |
                    <%= Html.ActionLink("Edit", "Edit", new { id = a.ID }) %> |
                    <% if (a.IsEnabled) { %>
                        <%= Html.ActionLink("Disable", "Disable", new { id = a.ID }, new { @class = "enable-disable-action" }) %>
                    <% } else { %>
                        <%= Html.ActionLink("Enable", "Enable", new { id = a.ID }, new { @class = "enable-disable-action" }) %>
                    <% } %>
                    |
                    <%= Html.ActionLink("Delete", "Delete", new { id = a.ID }, new { @class = "toolbar-button delete-action" }) %>
                </td>
            </tr>
        <% } %>
    </tbody>
</table>

<% var paged = Model as Health.Direct.Admin.Console.Models.Pagination.PaginatedList<AddressModel>; %>
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
            .click(function(event) { confirmDelete(event, $('#confirm-dialog'), $(this), 'Are you sure want to delete this address?', 'Address') });

        $('a.view-details').click(function(event) {
            showDetailsDialog($('#address-dialog'), event, $(this), 'Address Details');
        });
    });
</script>
