namespace Auto_Sync
{
    public class Serial_GeneratorBaseBase : IDisposable
    {
        private bool disposedValue;
        private System.ComponentModel.IContainer components = null;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (components != null)
                    {
                        components.Dispose();
                    }
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}