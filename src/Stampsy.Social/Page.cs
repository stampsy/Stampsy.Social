namespace Stampsy.Social
{
    public class Page<T>
    {
        public T Value { get; private set; }
        public string NextPageToken { get; private set; }

        public bool HasNextPage {
            get { return NextPageToken != null; }
        }

        public Page (T value, string nextPageToken)
        {
            Value = value;
            NextPageToken = nextPageToken;
        }
    }
}