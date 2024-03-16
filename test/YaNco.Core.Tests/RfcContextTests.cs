using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dbosoft.YaNco;
using LanguageExt;
using Moq;
using Xunit;
using YaNco.Core.Tests.RfcMock;

namespace YaNco.Core.Tests
{
    public class RfcContextTests
    {

        [Fact]
        public async Task Connection_Is_Created()
        {
            var connectionIO = new Mock<SAPRfcConnectionIO>();
            connectionIO.SetupOpenConnection(out _);
            var connFunc = new ConnectionBuilder(new Dictionary<string, string>())
                .ConfigureRuntime(c => c.UseSettingsFactory((l, m, o) =>
                    new SAPRfcRuntimeSettings(l, m, o)
                    {
                        RfcConnectionIO = connectionIO.Object,
                        RfcFunctionIO = new Mock<SAPRfcFunctionIO>().Object
                    })).Build();


            using (var rfcContext = new RfcContext(connFunc))
            {
                await rfcContext.GetConnection().IfLeft(l => l.Throw());

            }

            connectionIO.VerifyAll();


        }

        [Fact]
        public async Task Function_Is_Called()
        {
            var connectionIO = new Mock<SAPRfcConnectionIO>();
            connectionIO.SetupOpenConnection(out var connHandle);
            var functionIO = new Mock<SAPRfcFunctionIO>();
            var dataIO = new Mock<SAPRfcDataIO>();
            functionIO.SetupFunction("A_FUNC", connHandle,
                (h) =>
                {
                    dataIO.Setup(x => x.GetFieldValue<string>(h, It.IsAny<Func<Either<RfcError, RfcFieldInfo>>>()
                        ))
                        .Returns("VALUE");
                });

            var connFunc = new ConnectionBuilder(new Dictionary<string, string>())
                .ConfigureRuntime(c => c.UseSettingsFactory((l, m, o) =>
                    new SAPRfcRuntimeSettings(l, m, o)
                    {
                        RfcConnectionIO = connectionIO.Object,
                        RfcFunctionIO = functionIO.Object,
                        RfcDataIO = dataIO.Object
                    })).Build();


            using (var rfcContext = new RfcContext(connFunc))
            {
                await rfcContext.CallFunction("A_FUNC",
                        f => f.GetField<string>("FIELD"),
                        f => f)
                    .IfLeft(l => { l.Throw(); });

            }

            connectionIO.VerifyAll();
            functionIO.VerifyAll();


        }
    }
}