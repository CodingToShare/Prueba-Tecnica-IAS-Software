# Legacy Code Review

Short review of the two legacy fragments in the challenge: the concrete risk and the minimum safe correction. The
fragments are not part of this repository, so the corrections below are representative code for the original
technologies (ASP.NET WebForms / ASP.NET MVC 5 on .NET Framework) and were not compiled here. No rewrite or migration
is proposed.

## Fragment A — WebForms query

```csharp
var sql = "SELECT Id, Customer, Status FROM ServiceRequests WHERE Customer = '" + txtCustomer.Text + "'";
```

**Risk: SQL injection.** The text box value is concatenated into the SQL statement, so input such as
`' OR 1=1 --` changes the query (reads every row, or worse with batched statements).

**Minimum safe correction:** a parameterized command. Parameterization — not string escaping — is the fix.

```csharp
const string sql = "SELECT Id, Customer, Status FROM ServiceRequests WHERE Customer = @customer";

using (var cmd = new SqlCommand(sql, cn))
{
    cmd.Parameters.Add("@customer", SqlDbType.NVarChar, 100).Value = txtCustomer.Text.Trim();
    using (var reader = cmd.ExecuteReader())
    {
        // bind results as before
    }
}
```

Disposing the command and reader is included because it costs nothing here; async/data-access refactoring would be a
separate improvement, not part of the minimum fix.

## Fragment B — MVC status update

```csharp
var request = db.ServiceRequests.Find(id);
request.Status = status;
db.SaveChanges();
return Json(request);
```

**Risks:**

1. `Find(id)` can return `null` → `NullReferenceException` (500) instead of 404.
2. `status` is unrestricted client input: any text is stored.
3. Business transitions are bypassed: a `Completed` request can be moved back (the rule this solution enforces).
4. The EF entity is serialized directly: exposes every column and can fail on lazy-loaded navigation properties.
5. A state-changing action should only accept POST with an anti-forgery token (if not already in place).

**Minimum safe correction** (same controller, no architectural change):

```csharp
private static readonly Dictionary<string, string[]> AllowedTransitions = new Dictionary<string, string[]>
{
    ["Pending"] = new[] { "Processing" },
    ["Processing"] = new[] { "Completed", "Failed" },
    ["Completed"] = new string[0], // terminal
    ["Failed"] = new string[0],
};

[HttpPost]
[ValidateAntiForgeryToken]
public ActionResult UpdateStatus(int id, string status)
{
    var request = db.ServiceRequests.Find(id);
    if (request == null)
        return HttpNotFound();

    if (status == null || !AllowedTransitions.ContainsKey(status))
        return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Unknown status.");

    string[] next;
    if (!AllowedTransitions.TryGetValue(request.Status ?? "", out next) || !next.Contains(status))
        return new HttpStatusCodeResult(HttpStatusCode.Conflict, "Transition not allowed.");

    request.Status = status;
    db.SaveChanges();

    return Json(new { request.Id, request.Status });
}
```

The response is a small anonymous DTO instead of the entity. In this repository the same rules live in the domain
entity (`ServiceRequest.MarkProcessing/Complete/Fail`), and the API never exposes a generic status update.
