using System;
using System.Text;

namespace GitHubProxy.Helper
{
    public class Utf8StringReplaceDirective
    {
        public Utf8StringReplaceDirective(string originalString, string newString)
        {
            if (originalString is null)
            {
                throw new ArgumentNullException(nameof(originalString));
            }

            if (newString is null)
            {
                throw new ArgumentNullException(nameof(newString));
            }

            OriginalString = Encoding.UTF8.GetBytes(originalString);
            NewString = Encoding.UTF8.GetBytes(newString);
        }

        public byte[] OriginalString { get; }
        public byte[] NewString { get; }
    }
}
