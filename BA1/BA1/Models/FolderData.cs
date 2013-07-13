using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BA1
{
    /// <summary>
    /// Organizes skydrive folder properties
    /// </summary>
    public class FolderFileData
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string Parent_id { get; set; }
    }

    /// <summary>
    /// Wrapper for Json
    /// </summary>
    public class FolderWrapper
    {
        public List<FolderFileData> Data { get; set; }
    }
}
