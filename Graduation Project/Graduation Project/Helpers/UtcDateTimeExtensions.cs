namespace Graduation_Project.Helpers
{
    public static class UtcDateTimeExtensions
    {
        /// <summary>
        /// Tags a value read from a *Utc column as UTC before it is sent to a client.
        /// EF returns these with DateTimeKind.Unspecified, which serializes without a
        /// "Z" suffix; browsers then parse the value as local time and shift it by the
        /// client's offset. Returning a DateTimeOffset keeps the offset in the payload.
        /// </summary>
        public static DateTimeOffset AsUtcOffset(this DateTime value)
        {
            var utc = value.Kind == DateTimeKind.Utc
                ? value
                : DateTime.SpecifyKind(value, DateTimeKind.Utc);

            return new DateTimeOffset(utc);
        }

        public static DateTimeOffset? AsUtcOffset(this DateTime? value)
            => value.HasValue ? value.Value.AsUtcOffset() : null;
    }
}
