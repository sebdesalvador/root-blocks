// using Newtonsoft.Json;
//
// namespace BuildingBlocks.Logging.Serilog.Extensions;
//
// public static class LoggerConfigurationExtensions
// {
//     /// <summary>
//     /// A workaround for https://github.com/saleem-mirza/serilog-sinks-azure-analytics/issues/49
//     /// What this does is:
//     ///  1) find the AzureLogAnalyticsSink via reflection
//     ///  2) get its JsonSerializer
//     ///  3) add the custom Exception types handling converter to it
//     /// </summary>
//     /// <param name="loggerConfiguration"></param>
//     public static void ApplyAzureAnalyticsSinkSerializerWorkaround( this LoggerConfiguration loggerConfiguration )
//     {
//         var eventSinkListField = loggerConfiguration.GetType().GetField( "_logEventSinks",
//                                                                          System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance );
//         var eventSinkList = ( List< ILogEventSink > )eventSinkListField!.GetValue( loggerConfiguration )!;
//         var logAnalyticsSink = eventSinkList!.FirstOrDefault( x => x.GetType().Name == "AzureLogAnalyticsSink" );
//
//         if ( logAnalyticsSink == null )
//         {
//             var restrictedSinks = eventSinkList.Where( x => x.GetType().Name == "RestrictedSink" );
//             var restrictedLogAnalyticsSink = restrictedSinks.FirstOrDefault( x => x.GetType()
//                                                                                    .GetField( "_sink",
//                                                                                               System.Reflection.BindingFlags.NonPublic
//                                                                                             | System.Reflection.BindingFlags.Instance )
//                                                                                   ?.GetValue( x )
//                                                                                   ?.GetType()
//                                                                                    .Name == "AzureLogAnalyticsSink" );
//
//             if ( restrictedLogAnalyticsSink != null )
//                 logAnalyticsSink = ( ILogEventSink )restrictedLogAnalyticsSink.GetType()
//                                                                               .GetField( "_sink",
//                                                                                          System.Reflection.BindingFlags.NonPublic
//                                                                                        | System.Reflection.BindingFlags.Instance )
//                                                                              ?.GetValue( restrictedLogAnalyticsSink )!;
//         }
//
//         if ( logAnalyticsSink == null ) return;
//
//         var jsonSerializer = ( JsonSerializer )logAnalyticsSink.GetType()
//                                                                .GetField( "_jsonSerializer",
//                                                                           System.Reflection.BindingFlags.NonPublic
//                                                                         | System.Reflection.BindingFlags.Instance )
//                                                               ?.GetValue( logAnalyticsSink )!;
//         jsonSerializer?.Converters.Add( new AzureAnalyticsExceptionJsonConverter() );
//     }
// }
