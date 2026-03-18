using HashidsNet;

namespace capstone_backend.Extensions.Common
{
    public static class IdEncoder
    {
        private static readonly Hashids _hashids = new Hashids(Environment.GetEnvironmentVariable("HASH_SALT") == null ? "17032026" : Environment.GetEnvironmentVariable("HASH_SALT"));

        public static string Encode(long id) => _hashids.EncodeLong(id);

        public static long Decode(string encodedId)
        {
            var decoded = _hashids.DecodeLong(encodedId);
            return decoded.Length > 0 ? decoded[0] : 0;
        }
    }
}
