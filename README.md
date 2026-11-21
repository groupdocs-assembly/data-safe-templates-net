# Templates that survive incomplete data

[![build](https://github.com/groupdocs-assembly/data-safe-templates-net/actions/workflows/build.yml/badge.svg)](https://github.com/groupdocs-assembly/data-safe-templates-net/actions/workflows/build.yml)
[![NuGet](https://img.shields.io/nuget/v/GroupDocs.Assembly?label=GroupDocs.Assembly)](https://www.nuget.org/packages/GroupDocs.Assembly/)
[![.NET](https://img.shields.io/badge/.NET-8.0%20%7C%2010.0-512bd4)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)

Real data has holes. A customer with no payment card yet, a loyalty record that is `null`, a field
an upstream system stopped sending last Tuesday. A report that fills in perfectly on the developer's
machine and fails on the third customer of the night batch is the normal way this goes wrong.

This sample shows the three things that decide what happens instead: a **JSON Schema** as the
contract between data and template (new in 26.11), and the two assembly options that turn a missing
member into an empty value or into a message inside the report.

```
dotnet run --project src/SafeTemplates -- all
```

![the report assembled from incomplete data](docs/output.png)

## Read this with

| Resource | What it adds |
|---|---|
| [Blog: Templates that survive incomplete data](https://blog.groupdocs.com/assembly/templates-that-survive-incomplete-data-csharp/) | the reasoning, with the output of each case |
| [Docs: Templates for incomplete data](https://docs.groupdocs.com/assembly/net/templates-for-incomplete-data/) | the reference page |
| [This repository](https://github.com/groupdocs-assembly/data-safe-templates-net) | the code that runs |

## What it prints

```
-- without a schema
   InvalidOperationException: An error has been encountered at the end of expression
   'customer.Cards.First().Type]>'. Can not get the value of member 'Type' on type 'System.Data.DataRow'.

-- with a schema
   wrote output/with-schema.docx

-- a member missing from the data
   default options: An error has been encountered at the end of expression 'customer.Fax]>'...
   AllowMissingMembers: Customer: Quayside Bistro

-- a broken tag in the template
   AssembleDocument returned False
```

Four cases, four outcomes, and the data is the same thin JSON throughout:

```json
{ "Name": "Quayside Bistro", "Account": "00042", "Cards": [], "Loyalty": null }
```

## 1. The schema is the contract

Without a schema the engine learns the shape of the data from the data itself. An empty array has
no element type, so a tag that reaches into it cannot be resolved even when the branch around it is
never rendered — and `"00042"` looks enough like a number to become `42`.

A schema settles both before the first value is read:

```csharp
JsonDataSource dataSource = new JsonDataSource(
    "data/Customer.json",
    "data/Customer.schema.json");

new DocumentAssembler().AssembleDocument(
    "templates/Json schema.docx",
    "output/with-schema.docx",
    new DataSourceInfo(dataSource, "customer"));
```

Supported since 26.11, for `JsonDataSource`. Members the schema declares can be used by the
template even when the data has them empty, null or missing.

## 2. `AllowMissingMembers`

A member the data does not have **at all**. By default that is an error; with the option it renders
as an empty value:

```csharp
DocumentAssembler lenient = new DocumentAssembler
{
    Options = DocumentAssemblyOptions.AllowMissingMembers
};
```

Use it when the data is genuinely sparse and a blank line is the right output. Leave it off when a
missing member means somebody changed the data and nobody noticed — that is the case where you want
the batch to stop and tell you.

## 3. `InlineErrorMessages`

For the template nobody on your team owns. The error is written into the report where the tag was,
and `AssembleDocument` returns `false`:

```csharp
DocumentAssembler inline = new DocumentAssembler
{
    Options = DocumentAssemblyOptions.InlineErrorMessages
};

bool success = inline.AssembleDocument(template, output, dataSourceInfo);
```

A nightly batch of two thousand documents finishes, and the three that are wrong carry the reason
in the text, next to the tag that caused it.

## Which to choose

| Situation | What to use |
|---|---|
| Data from a system that may omit fields, shape known in advance | a JSON Schema |
| Data genuinely sparse, a blank is the right output | `AllowMissingMembers` |
| Templates edited by people outside your team | `InlineErrorMessages` and check the return value |
| A missing member means something upstream broke | the default: let it throw |

## Layout

```
src/SafeTemplates/Program.cs   the four cases
templates/                     Json schema.docx, Missing member.txt, Syntax error.txt
data/                          Customer.json, Customer.schema.json
```

Every call mirrors an example of the product's examples repository
([GroupDocs.Assembly-for-.NET](https://github.com/groupdocs-assembly/GroupDocs.Assembly-for-.NET)).

## Notes

* **Licence.** Without one the library runs in evaluation mode and the output carries evaluation
  marks ([temporary licence](https://purchase.groupdocs.com/temporary-license)).
* **Frameworks.** The sample targets .NET 8; the package also builds for .NET Framework 4.0 Client
  Profile, 4.6.2 and 4.8, .NET Standard 2.0, .NET 6 and .NET 10.

## Related samples

* [llm-json-to-branded-documents-net](https://github.com/groupdocs-assembly/llm-json-to-branded-documents-net) — where incomplete data comes from most often
* [word-report-from-template-net](https://github.com/groupdocs-assembly/word-report-from-template-net) — the basics, if you are new to the library
* [GroupDocs.Assembly-for-.NET](https://github.com/groupdocs-assembly/GroupDocs.Assembly-for-.NET) — the full example set

## License

MIT, see [LICENSE](LICENSE). GroupDocs.Assembly itself is a commercial library with its own
[licence terms](https://purchase.groupdocs.com/policies/).
