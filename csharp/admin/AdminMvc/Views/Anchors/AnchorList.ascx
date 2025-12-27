<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl<IEnumerable<AnchorModel>>" %>
<%@ Import Namespace="Health.Direct.Admin.Console.Common"%>
<%@ Import Namespace="Health.Direct.Admin.Console.Models"%>

<table class="grid ui-widget ui-widget-content">
    <thead class="ui-widget-header">
        <tr>
            <th>Owner</th>
            <th>Thumbprint</th>
            <th>Status</th>
            <th>Created On</th>
            <th>Valid From</th>
            <th>Valid Until</th>
            <th>Purpose</th>
            <th>Actions</th>
        </tr>
    </thead>
    <tbody>
        <% foreach (var a in Model) { %>
            <tr>
                <td><%= a.Owner %></td>
                <td><%= Html.P(a.Thumbprint, new { title = a.Thumbprint, @class = "thumbprint" }) %></td>
                <td class="status"><%= a.Status %></td>
                <td><%= Html.Span(Formatter.Format(a.CreateDate), new { title = a.CreateDate }) %></td>
                <td><%= Html.Span(Formatter.Format(a.ValidStartDate), new { title = a.ValidStartDate }) %></td>
                <td><%= Html.Span(Formatter.Format(a.ValidEndDate), new { title = a.ValidEndDate }) %></td>
                <td><%= a.Purpose %></td>
                <td>
                    <%= Html.ActionLink("View", "Details", new { a.ID }, new { @class = "view-details" }) %> |
                    <% if (a.IsEnabled) { %>
                        <%= Html.ActionLink("Disable", "Disable", new { a.ID }, new { @class = "enable-disable-action" }) %>
                    <% } else { %>
                        <%= Html.ActionLink("Enable", "Enable", new { a.ID }, new { @class = "enable-disable-action" }) %>
                    <% } %>
                    |
                    <%= Html.ActionLink("Delete", "Delete", new { a.ID }, new { @class = "toolbar-button delete-action" }) %>
                </td>
            </tr>
        <% } %>
    </tbody>
</table>

<% var paged = Model as Health.Direct.Admin.Console.Models.Pagination.PaginatedList<AnchorModel>; %>
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
            .click(function(event) { confirmDelete(event, $('#confirm-dialog'), $(this), 'Are you sure want to delete this anchor?', 'Anchor') });

        $('a.view-details').click(function(event) {
            showDetailsDialog($('#anchor-dialog'), event, $(this), 'Anchor Details');
        });
    });
</script>
