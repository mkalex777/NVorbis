using NVorbis.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NVorbis
{
    internal class TagData : ITagData
    {
        static IReadOnlyList<string> s_emptyList = new List<string>().ToReadOnlyList();

        Dictionary<string, IList<string>> _tags;

        public TagData(string vendor, string[] comments)
        {
            EncoderVendor = vendor;

            var tags = new Dictionary<string, IList<string>>();
            for (var i = 0; i < comments.Length; i++)
            {
                var parts = comments[i].Split('=');
                if (parts.Length == 1)
                {
                    parts = new[] { parts[0], string.Empty };
                }

                var bktIdx = parts[0].IndexOf('[');
                if (bktIdx > -1)
                {
                    parts[1] = parts[0].Substring(bktIdx + 1, parts[0].Length - bktIdx - 2)
                                       .ToUpper(System.Globalization.CultureInfo.CurrentCulture)
                                     + ": "
                                     + parts[1];
                    parts[0] = parts[0].Substring(0, bktIdx);
                }
                IList<string> list;
                if (tags.TryGetValue(parts[0].ToUpperInvariant(), out list))
                {
                    list.Add(parts[1]);
                }
                else
                {
                    tags.Add(parts[0].ToUpperInvariant(), new List<string> { parts[1] });
                }
            }
            _tags = tags;
        }

        public string GetTagSingle(string key, bool concatenate = false)
        {
            var values = GetTagMulti(key);
            if (values.Count > 0)
            {
                if (concatenate)
                {
                    return string.Join(Environment.NewLine, values.ToArray());
                }
                return values[values.Count - 1];
            }
            return string.Empty;
        }

        public IReadOnlyList<string> GetTagMulti(string key)
        {
            IList<string> values;
            if (_tags.TryGetValue(key.ToUpperInvariant(), out values))
            {
                return (IReadOnlyList<string>)values;
            }
            return s_emptyList;
        }

        public Dictionary<string, IList<string>> All { get { return _tags; } }

        public string EncoderVendor { get; private set; }

        public string Title { get { return GetTagSingle("TITLE"); } }

        public string Version { get { return GetTagSingle("VERSION"); } }

        public string Album { get { return GetTagSingle("ALBUM"); } }

        public string TrackNumber { get { return GetTagSingle("TRACKNUMBER"); } }

        public string Artist { get { return GetTagSingle("ARTIST"); } }

        public IReadOnlyList<string> Performers { get { return GetTagMulti("PERFORMER"); } }

        public string Copyright { get { return GetTagSingle("COPYRIGHT"); } }

        public string License { get { return GetTagSingle("LICENSE"); } }

        public string Organization { get { return GetTagSingle("ORGANIZATION"); } }

        public string Description { get { return GetTagSingle("DESCRIPTION"); } }

        public IReadOnlyList<string> Genres { get { return GetTagMulti("GENRE"); } }

        public IReadOnlyList<string> Dates { get { return GetTagMulti("DATE"); } }

        public IReadOnlyList<string> Locations { get { return GetTagMulti("LOCATION"); } }

        public string Contact { get { return GetTagSingle("CONTACT"); } }

        public string Isrc { get { return GetTagSingle("ISRC"); } }
    }
}