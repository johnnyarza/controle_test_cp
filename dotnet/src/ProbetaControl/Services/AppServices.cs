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
            SpecimenPhotos = new SpecimenPhotoService(new SpecimenPhotoRepository(db, session), session);
            var appSettings = new AppSettingsRepository(db, session);
            ReportImages = new ReportImageService(appSettings, session);
            ReportSettings = new ReportSettingsService(appSettings, ReportImages, session);
            LabelSettings = new LabelSettingsService(appSettings, session);
            Reports = new Reports.ReportService(Tests, Designs, Specimens, Clients, Providers, Materials, ReportSettings, LabelSettings, Clock);
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

        /// <summary>Photos of probetas (probeta dialog; later the phone page).</summary>
        public SpecimenPhotoService SpecimenPhotos { get; }

        public ReportImageService ReportImages { get; }

        /// <summary>Lab details printed on reports (Configuración → Datos del informe).</summary>
        public ReportSettingsService ReportSettings { get; }

        /// <summary>QR label server name and sheet (Configuración → Etiquetas).</summary>
        public LabelSettingsService LabelSettings { get; }

        /// <summary>Builds the printed reports and the QR labels.</summary>
        public Reports.ReportService Reports { get; }
    }
}
