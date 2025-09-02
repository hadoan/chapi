using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Chapi.AI.Utilities;
using Microsoft.SemanticKernel;
using Microsoft.Extensions.Logging;

namespace Chapi.AI.Plugins.RunPack
{
    public sealed class RunPackPlugin
    {

        private readonly List<(string Path, string Content)> _files = new();

        public IReadOnlyList<(string Path, string Content)> Files => _files;

        [KernelFunction]
        [Description("Add a file to the downloadable run pack")]
        public string AddFile(
            [Description("File path to create, e.g., tests/email-service/smoke.sh")]
            [Required] string path,
            [Description("UTF-8 content of the file. For test files, this MUST be JSON in the 'chapi-test-1' schema.")]
            [Required] string content)
        {
            this.Add(path, content ?? "");
            return $"added:{path}";
        }


        [KernelFunction]
        [Description("Zip files")]
        public byte[] ZipFiles()
        {
            var zip = this.ToZip();
            System.Diagnostics.Debug.WriteLine($"zipped:{zip.Length} bytes");
            return zip;
        }



        public int GetFileCount() => _files.Count;

        public void Add(string path, string content)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("path required");
            _files.Add((Normalize(path), content ?? ""));
        }

        public byte[] ToZip(bool includeHelpers = true)
        {
            using var ms = new MemoryStream();
            using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
            {
                foreach (var (path, content) in _files)
                {
                    var entry = zip.CreateEntry(path, CompressionLevel.Optimal);
                    using var s = entry.Open();
                    var bytes = Encoding.UTF8.GetBytes(content);
                    s.Write(bytes, 0, bytes.Length);
                }

                if (includeHelpers)
                {
                    AddText(zip, "run.sh",
    "#!/usr/bin/env bash\nset -euo pipefail\nnode scripts/run-chapi-tests.js " + string.Join(" ", _files.Select(f => f.Path)));

                    AddText(zip, "run.ps1",
    "param([string[]]$files)\n$env:BASE_URL = $env:BASE_URL -as [string] ?? 'http://localhost:8080'\nnode scripts/run-chapi-tests.js $files");

                    AddText(zip, ".env.example",
    "BASE_URL=http://localhost:8080\nTOKEN=your_token_here\nEMAIL=test@example.com");

                    AddText(zip, "scripts/run-chapi-tests.js",
    "// tiny demo runner (mock). Replace with your CLI later.\nconst fs = require('fs'); const fetch = (...a)=>import('node-fetch').then(({default:f})=>f(...a));\nconst files = process.argv.slice(2); const BASE_URL = process.env.BASE_URL || 'http://localhost:8080';\nconst TOKEN = process.env.TOKEN || ''; const EMAIL = process.env.EMAIL || 'test@example.com';\n\n(async ()=>{ for(const f of files){ const j=JSON.parse(fs.readFileSync(f,'utf8'));\nfor(const t of j.tests){ const url=(t.request.url||'').replace('{{BASE_URL}}',BASE_URL).replace('{{id}}','1').replace('{{email}}',EMAIL);\nconst headers=Object.assign({},t.request.headers||{}); if((headers.Authorization||'').includes('{{TOKEN}}')) headers.Authorization=`Bearer ${TOKEN}`;\nconst res=await fetch(url,{method:t.request.method,headers,body:t.request.body?JSON.stringify(t.request.body):undefined});\nif(res.status!==t.expect.status){ console.error('FAIL',t.name,res.status); process.exitCode=1; } else { console.log('PASS',t.name); } } } })();");
                }
            }
            return ms.ToArray();
        }

        private static void AddText(ZipArchive zip, string path, string content)
        {
            var entry = zip.CreateEntry(Normalize(path), CompressionLevel.Optimal);
            using var s = new StreamWriter(entry.Open(), new UTF8Encoding(false));
            s.Write(content);
        }

        private static string Normalize(string p) => p.Replace("\\", "/").TrimStart('/');
    }
}
