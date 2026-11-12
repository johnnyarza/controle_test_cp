using ProbetaControl.Data;

namespace ProbetaControl.Services
{
    /// <summary>
    /// Composition root: builds every repository and service once the connection settings are known.
    /// View models receive what they need from here (no DI framework).
    /// </summary>
    public sealed class AppServices
    {
        public AppServices(ConnectionSettings settings, AppSession session, IClock clock = null)
        {
            Settings = settings;
            Session = session;
            Clock = clock ?? SystemClock.Instance;

            var db = new DbConnectionFactory(settings.BuildConnectionString());
            Users = new UserRepository(db, session);

            Auth = new AuthService(Users, session);
            UserAdmin = new UserService(Users, session);
            Clients = new ClientService(new ClientRepository(db, session), session);
            Providers = new ProviderService(new ProviderRepository(db, session), session);
            Materials = new MaterialService(new MaterialRepository(db, session), session);
            Designs = new ConcreteDesignService(new ConcreteDesignRepository(db, session), session);
            Tests = new CompressionTestService(new CompressionTestRepository(db, session), Clock, session);
            Specimens = new SpecimenService(new SpecimenRepository(db, session), Clock, session);
            ReportImages = new ReportImageService(new AppSettingsRepository(db, session), session);
            ServerInfo = new ServerInfoRepository(db, session);
        }

        /// <summary>Server and schema version ("Acerca de").</summary>
        public ServerInfoRepository ServerInfo { get; }

        public ConnectionSettings Settings { get; }

        public AppSession Session { get; }

        public IClock Clock { get; }

        public UserRepository Users { get; }

        public AuthService Auth { get; }

        public UserService UserAdmin { get; }

        public ClientService Clients { get; }

        public ProviderService Providers { get; }

        public MaterialService Materials { get; }

        public ConcreteDesignService Designs { get; }

        public CompressionTestService Tests { get; }

        public SpecimenService Specimens { get; }

        public ReportImageService ReportImages { get; }
    }
}
