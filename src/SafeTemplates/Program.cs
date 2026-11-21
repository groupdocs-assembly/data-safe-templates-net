using System;
using System.IO;
using System.Text;
using GroupDocs.Assembly;
using GroupDocs.Assembly.Data;

namespace SafeTemplates
{
    /// <summary>
    /// Templates that survive incomplete data: a JSON Schema as the contract, and the two options
    /// that decide what a missing member does. The calls mirror the official examples repository
    /// (GroupDocs.Assembly-for-.NET/Examples/.../AdvancedUsage/AssembleFromJsonWithSchema.cs and
    /// HandleTemplateErrors.cs).
    /// </summary>
    public static class Program
    {
        public static int Main(string[] args)
        {
            ApplyLicense();

            string command = args.Length == 0 ? "all" : args[0].ToLowerInvariant();

            try
            {
                switch (command)
                {
                    case "without-schema": WithoutSchema(); return 0;
                    case "with-schema": WithSchema(); return 0;
                    case "missing-member": MissingMember(); return 0;
                    case "inline-errors": InlineErrors(); return 0;
                    case "all":
                        WithoutSchema();
                        WithSchema();
                        MissingMember();
                        InlineErrors();
                        return 0;
                    case "-h":
                    case "--help": Usage(); return 0;
                    default:
                        Console.Error.WriteLine($"Unknown command '{command}'.");
                        Usage();
                        return 1;
                }
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine("Error: " + exception.Message);
                return 1;
            }
        }

        // ------------------------------------------------------------------ the problem

        /// <summary>
        /// The data is valid and the template is correct, and it still does not assemble.
        ///
        /// <c>Customer.json</c> has <c>"Cards": []</c> and <c>"Loyalty": null</c>. Without a schema
        /// the engine learns the shape of the data from the data itself, so it has no type for the
        /// elements of an empty array: a tag such as <c>&lt;&lt;[customer.Cards.First().Type]&gt;&gt;</c>
        /// cannot be resolved even though the branch around it is never rendered.
        /// </summary>
        public static void WithoutSchema()
        {
            Console.WriteLine("-- without a schema");

            try
            {
                new DocumentAssembler().AssembleDocument(
                    RepositoryPaths.Template("Json schema.docx"),
                    RepositoryPaths.OutputFile("without-schema.docx"),
                    new DataSourceInfo(new JsonDataSource(RepositoryPaths.DataFile("Customer.json")), "customer"));

                Console.WriteLine("   assembled");
            }
            catch (Exception exception)
            {
                Console.WriteLine("   " + exception.GetType().Name + ": " + FirstLine(exception.Message));
            }

            Console.WriteLine();
        }

        // ------------------------------------------------------------------ the contract

        /// <summary>
        /// The same template and the same data, with the schema that describes them. Supported
        /// since 26.11.
        ///
        /// Members declared in the schema can be used even when the data has them empty, null or
        /// missing, and their types come from the schema rather than from the first value that
        /// happens to be present: <c>"Account": "00042"</c> stays the string <c>00042</c> instead
        /// of becoming the number 42.
        /// </summary>
        public static void WithSchema()
        {
            Console.WriteLine("-- with a schema");

            JsonDataSource dataSource = new JsonDataSource(
                RepositoryPaths.DataFile("Customer.json"),
                RepositoryPaths.DataFile("Customer.schema.json"));

            new DocumentAssembler().AssembleDocument(
                RepositoryPaths.Template("Json schema.docx"),
                RepositoryPaths.OutputFile("with-schema.docx"),
                new DataSourceInfo(dataSource, "customer"));

            Console.WriteLine("   wrote " + RepositoryPaths.OutputFile("with-schema.docx"));
            Console.WriteLine();
        }

        // ------------------------------------------------------------------ the two options

        /// <summary>
        /// A member the data does not have at all. By default this is an error; with
        /// <see cref="DocumentAssemblyOptions.AllowMissingMembers"/> it renders as an empty value.
        ///
        /// Use it when the data is genuinely sparse and an empty line is the right output. Leave
        /// it off when a missing member means somebody changed the data and nobody noticed.
        /// </summary>
        public static void MissingMember()
        {
            Console.WriteLine("-- a member missing from the data");

            string template = RepositoryPaths.Template("Missing member.txt");
            string output = RepositoryPaths.OutputFile("missing-member.txt");

            try
            {
                new DocumentAssembler().AssembleDocument(template, output, new DataSourceInfo(CustomerData(), "customer"));
            }
            catch (InvalidOperationException exception)
            {
                Console.WriteLine("   default options: " + FirstLine(exception.Message));
            }

            DocumentAssembler lenient = new DocumentAssembler { Options = DocumentAssemblyOptions.AllowMissingMembers };
            lenient.AssembleDocument(template, output, new DataSourceInfo(CustomerData(), "customer"));

            Console.WriteLine("   AllowMissingMembers: " + FirstLine(File.ReadAllText(output)));
            Console.WriteLine();
        }

        /// <summary>
        /// A template somebody edited by hand and broke.
        ///
        /// With <see cref="DocumentAssemblyOptions.InlineErrorMessages"/> the error is written into
        /// the report where the tag was and <c>AssembleDocument</c> returns <c>false</c>, so a batch
        /// can finish and hand the broken documents to whoever owns the template.
        /// </summary>
        public static void InlineErrors()
        {
            Console.WriteLine("-- a broken tag in the template");

            string output = RepositoryPaths.OutputFile("inline-errors.txt");

            DocumentAssembler inline = new DocumentAssembler { Options = DocumentAssemblyOptions.InlineErrorMessages };
            bool success = inline.AssembleDocument(
                RepositoryPaths.Template("Syntax error.txt"),
                output,
                new DataSourceInfo(CustomerData(), "customer"));

            Console.WriteLine($"   AssembleDocument returned {success}");
            Console.WriteLine("   " + FirstLine(File.ReadAllText(output)));
            Console.WriteLine();
        }

        // ------------------------------------------------------------------ plumbing

        private static JsonDataSource CustomerData()
        {
            return new JsonDataSource(
                new MemoryStream(Encoding.UTF8.GetBytes("{ \"Name\": \"Quayside Bistro\", \"Total\": 1234.5 }")));
        }

        private static string FirstLine(string value)
        {
            string trimmed = value.Replace("\r", string.Empty).Trim();
            int newLine = trimmed.IndexOf('\n');
            return newLine < 0 ? trimmed : trimmed.Substring(0, newLine);
        }

        /// <summary>Optional: without a licence the output carries evaluation marks.</summary>
        private static void ApplyLicense()
        {
            string? path = Environment.GetEnvironmentVariable("GROUPDOCS_ASSEMBLY_LICENSE")?.Trim();
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            if (File.Exists(path))
            {
                new License().SetLicense(path);
            }
            else
            {
                Console.Error.WriteLine("GROUPDOCS_ASSEMBLY_LICENSE points at a missing file: " + path);
            }
        }

        private static void Usage()
        {
            Console.WriteLine("Templates that survive incomplete data - GroupDocs.Assembly for .NET");
            Console.WriteLine();
            Console.WriteLine("  without-schema   what an empty array and a null do without a schema");
            Console.WriteLine("  with-schema      the same data described by a JSON Schema");
            Console.WriteLine("  missing-member   AllowMissingMembers");
            Console.WriteLine("  inline-errors    InlineErrorMessages");
            Console.WriteLine("  all              all four (the default)");
            Console.WriteLine();
            Console.WriteLine("  dotnet run --project src/SafeTemplates -- all");
        }
    }
}
