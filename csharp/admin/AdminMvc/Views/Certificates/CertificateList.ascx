<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl<IEnumerable<CertificateModel>>" %>
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
            <th>Actions</th>
        </tr>
    </thead>
    <tbody>
        <% foreach (var c in Model) { %>
            <tr>
                <td><%= c.Owner %></td>
                <td><%= Html.P(c.Thumbprint, new { title = c.Thumbprint, @class = "thumbprint" }) %></td>
                <td class="status"><%= c.Status %></td>
                <td><%= Html.Span(Formatter.Format(c.CreateDate), new { title = c.CreateDate }) %></td>
                <td><%= Html.Span(Formatter.Format(c.ValidStartDate), new { title = c.ValidStartDate }) %></td>
                <td><%= Html.Span(Formatter.Format(c.ValidEndDate), new { title = c.ValidEndDate }) %></td>
                <td>
                    <%= Html.ActionLink("View", "Details", new { c.ID }, new { @class = "view-details" }) %> |
                    <% if (c.IsEnabled) { %>
                        <%= Html.ActionLink("Disable", "Disable", new { c.ID }, new { @class = "enable-disable-action" }) %>
                    <% } else { %>
                        <%= Html.ActionLink("Enable", "Enable", new { c.ID }, new { @class = "enable-disable-action" }) %>
                    <% } %>
                    |
                    <%= Html.ActionLink("Delete", "Delete", new { c.ID }, new { @class = "toolbar-button delete-action" }) %>
                </td>
            </tr>
        <% } %>
    </tbody>
</table>

<% var paged = Model as Health.Direct.Admin.Console.Models.Pagination.PaginatedList<CertificateModel>; %>
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
            .click(function(event) { confirmDelete(event, $('#confirm-dialog'), $(this), 'Are you sure want to delete this certificate?', 'Certificate') });

        $('a.view-details').click(function(event) {
            showDetailsDialog($('#certificate-dialog'), event, $(this), 'Certificate Details');
        });
    });
</script>
