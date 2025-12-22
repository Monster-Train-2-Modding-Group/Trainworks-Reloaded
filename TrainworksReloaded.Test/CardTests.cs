using HarmonyLib;
using Microsoft.Extensions.Configuration;
using Moq;
using SimpleInjector;
using TrainworksReloaded.Base.Card;
using TrainworksReloaded.Base.Localization;
using TrainworksReloaded.Core.Enum;
using TrainworksReloaded.Core.Impl;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Test
{
    public class CardTests : IDisposable
    {
        public Container Container { get; set; }
        public Guid TestGuid { get; set; }
        public Dictionary<string, LocalizationTerm> TermDictionary { get; set; }
        public List<(LogLevel Level, object Message)> LoggedMessages { get; set; }

        public CardTests()
        {
            Container = new Container();

            //Atlas
            var atlas = new PluginAtlas();
            var configuration = new ConfigurationBuilder();
            var basePath = Path.GetDirectoryName(this.GetType().Assembly.Location);
            configuration.SetBasePath(basePath!);
            configuration.AddJsonFile("examples/cards/fire_starter.json");
            var definition = new PluginDefinition(configuration.Build());
            atlas.PluginDefinitions.Add("test_plugin", definition);
            Container.RegisterInstance<PluginAtlas>(atlas);

            //Guid
            TestGuid = new Guid();
            var guidProvider = new Mock<IGuidProvider>();
            guidProvider.Setup(xs => xs.GetGuidDeterministic(It.IsAny<string>())).Returns(TestGuid);
            Container.RegisterInstance<IGuidProvider>(guidProvider.Object);

            //Instance Generator
            Container.RegisterConditional(
                typeof(IInstanceGenerator<>),
                typeof(InstanceGenerator<>),
                c => !c.Handled
            );

            // Term Register
            TermDictionary = new Dictionary<string, LocalizationTerm>();
            var termRegister = new Mock<IRegister<LocalizationTerm>>();

            // Register Method
            termRegister
                .Setup(tr => tr.Register(It.IsAny<string>(), It.IsAny<LocalizationTerm>()))
                .Callback<string, LocalizationTerm>((key, term) => TermDictionary[key] = term);

            // TryLookupName
            termRegister
                .Setup(tr =>
                    tr.TryLookupIdentifier(
                        It.IsAny<string>(),
                        It.IsAny<RegisterIdentifierType>(),
                        out It.Ref<LocalizationTerm?>.IsAny,
                        out It.Ref<bool?>.IsAny
                    )
                )
                .Returns(
                    (string name, RegisterIdentifierType identifierType, out LocalizationTerm? term, out bool? isModded) =>
                    {
                        term = TermDictionary.Values.FirstOrDefault(t => t.English == name);
                        isModded = false;
                        return term != null;
                    }
                );

            // TryLookupId
            termRegister
                .Setup(tr =>
                    tr.TryLookupIdentifier(
                        It.IsAny<string>(),
                        It.IsAny<RegisterIdentifierType>(),
                        out It.Ref<LocalizationTerm?>.IsAny,
                        out It.Ref<bool?>.IsAny
                    )
                )
                .Returns(
                    (string id, RegisterIdentifierType identifierType, out LocalizationTerm? term, out bool? isModded) =>
                    {
                        term = TermDictionary.ContainsKey(id) ? TermDictionary[id] : null;
                        isModded = false;
                        return term != null;
                    }
                );

            Container.RegisterInstance<IRegister<LocalizationTerm>>(termRegister.Object);

            // Initialize log storage
            LoggedMessages = new List<(LogLevel, object)>();

            // Mock IModLogger<T>
            var mockLogger = new Mock<IModLogger<CardDataPipeline>>();

            // Capture log messages in a list for assertions
            mockLogger
                .Setup(logger => logger.Log(It.IsAny<LogLevel>(), It.IsAny<object>()))
                .Callback<LogLevel, object>(
                    (level, data) =>
                    {
                        LoggedMessages.Add((level, data));
                    }
                );

            Container.RegisterInstance<IModLogger<CardDataPipeline>>(mockLogger.Object);

            Container.Register<CardDataPipeline>();
        }

        public void Dispose() { }

        [Fact]
        public void Run_ShouldLoadCardsSuccessfully()
        {
            // Arrange
            var pipeline = Container.GetInstance<CardDataPipeline>();
            var mockCardRegister = new Mock<IRegister<CardData>>();

            // Act
            var results = pipeline.Run(mockCardRegister.Object);

            // Assert
            Assert.NotEmpty(results);
            var cardData = results[0];

            Assert.Equal("FireStarter", cardData.Id);
            Assert.Equal("test_plugin-Card-FireStarter", cardData.Data.name);

            // Check localization terms
            Assert.True(
                TermDictionary.ContainsKey("CardData_nameKey-test_plugin-Card-FireStarter")
            );
            Assert.Equal(
                "Firestarter",
                TermDictionary["CardData_nameKey-test_plugin-Card-FireStarter"].English
            );

            Assert.True(
                TermDictionary.ContainsKey("CardData_descriptionKey-test_plugin-Card-FireStarter")
            );
            Assert.Equal(
                "Deal [effect0.power] damage then apply [pyregel] [effect1.status0.power].",
                TermDictionary["CardData_descriptionKey-test_plugin-Card-FireStarter"].English
            );

            // Verify logger captured messages
            Assert.DoesNotContain(LoggedMessages, log => log.Level == LogLevel.Error);
        }

        [Fact]
        public void Run_ShouldHandleMissingIdGracefully()
        {
            // Arrange - Add invalid configuration
            var atlas = Container.GetInstance<PluginAtlas>();
            atlas.PluginDefinitions["test_plugin"].Configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        { "cards:0:names:english", "Unnamed Card" }, // Missing "id"
                    }
                )
                .Build();

            var pipeline = Container.GetInstance<CardDataPipeline>();
            var mockCardRegister = new Mock<IRegister<CardData>>();

            // Act
            var results = pipeline.Run(mockCardRegister.Object);

            // Assert
            Assert.Empty(results);

            // Ensure no unexpected logs
            Assert.DoesNotContain(LoggedMessages, log => log.Level == LogLevel.Error);
        }

        [Fact]
        public void LoadCardConfiguration_ShouldRegisterNewCardCorrectly()
        {
            // Arrange
            var mockConfig = new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        { "id", "fire_starter" },
                        { "names:english", "Fire Starter" },
                        { "descriptions:english", "Starts fires" },
                        { "cost", "3" },
                    }
                )
                .Build();

            var pipeline = Container.GetInstance<CardDataPipeline>();
            var mockCardRegister = new Mock<IRegister<CardData>>();

            // Act
            var result = pipeline.LoadCardConfiguration(
                mockCardRegister.Object,
                "test_plugin",
                mockConfig
            );

            // Assert
            Assert.NotNull(result);
            var cardDataDefinition = (CardDataDefinition)result!;

            Assert.Equal("fire_starter", cardDataDefinition.Id);
            Assert.Equal(
                3,
                AccessTools.Field(typeof(CardData), "cost").GetValue(cardDataDefinition.Data)
            );

            // Verify localization term registration
            Assert.True(
                TermDictionary.ContainsKey("CardData_nameKey-test_plugin-Card-fire_starter")
            );
            Assert.True(
                TermDictionary.ContainsKey("CardData_descriptionKey-test_plugin-Card-fire_starter")
            );
        }

        [Fact]
        public void LoadCardConfiguration_ShouldLogWhenOverridingExistingCard()
        {
            // Arrange - Mock card lookup
            var existingCard = new CardData();

            var mockCardRegister = new Mock<IRegister<CardData>>();
            CardData? ignoredCard = null;
            bool? ignoredModded = null;

            mockCardRegister
                .Setup(cr =>
                    cr.TryLookupIdentifier(
                        "fire_starter",
                        It.IsAny<RegisterIdentifierType>(),
                        out ignoredCard,
                        out ignoredModded
                    )
                )
                .Callback((
                    string _,
                    RegisterIdentifierType _,
                    out CardData? card,
                    out bool? modded
                ) =>
                {
                    card = existingCard;
                    modded = true;
                })
                .Returns(true);

            var mockConfig = new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        { "id", "fire_starter" },
                        { "override", "true" },
                        { "names:english", "Fire Starter" },
                    }
                )
                .Build();

            var pipeline = Container.GetInstance<CardDataPipeline>();

            // Act
            pipeline.LoadCardConfiguration(mockCardRegister.Object, "test_plugin", mockConfig);

            // Assert
            Assert.Contains(
                LoggedMessages,
                log =>
                    log.Level == LogLevel.Info
                    && log.Message.ToString()!.Contains("Overriding Card fire_starter")
            );
        }
    }
}
