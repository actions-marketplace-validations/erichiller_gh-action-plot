using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace PlotGitHubAction;

public static class RepoAnalysis {
    public static void Run( ActionConfig config ) {
        StringBuilder readme = new StringBuilder();

        StringBuilder links = new StringBuilder();

        if ( config.TestResultsDir is { } ) {
            var testResultAnalyzer = new TestResultAnalyzer( config );
            testResultAnalyzer.ScanForTrx();
            links.AppendLine( $"- [Test Results]({relPath( testResultAnalyzer.MarkdownSummaryFilePath )})" );
            readme.Append( Regex.Replace( testResultAnalyzer.Sb.ToString(), "^#", "##" ) );
        }

        if ( config.IsCoverageHistoryEnabled && config.CoverageHistoryDir is { } ) {
            var coverageHistoryPlotter = new CoverageHistoryPlotter( config.CoverageHistoryDir );
            var coveragePlot           = coverageHistoryPlotter.ScanAndPlot();
            PlotGen.CreatePlot(
                JsonSerializer.Serialize( coveragePlot, PlotGen.SERIALIZER_OPTIONS ),
                config.PlotOutputDir );
            readme.AppendLine( "\n## Coverage\n\n" );
            readme.AppendLine( config.GetMarkdownChartLink( CoverageHistoryPlotter.CHART_OUTPUT_PATH ) );
        }

        if ( config.IsTodoScanEnabled ) {
            var todoScanner = new TodoScanner( config );
            todoScanner.ScanForTodos();
            PlotGen.CreatePlot(
                JsonSerializer.Serialize( todoScanner.GetPlottable(), PlotGen.SERIALIZER_OPTIONS ),
                config.PlotOutputDir );
            links.AppendLine( $"- [TO-DOs]({relPath( todoScanner.MarkdownOutputPath )})" );
            readme.AppendLine( "\n## To-Do\n\n" );
            readme.AppendLine( config.GetMarkdownChartLink( TodoScanner.CHART_FILENAME_TOTAL ) );
        }

        if ( config.SourceScanDir is { } ) {
            var lineCounter = new FileLineCounter( config );
            lineCounter.Analyze();
            var lineCountPlottable = lineCounter.GetPlottable();
            PlotGen.CreatePlot(
                JsonSerializer.Serialize( lineCountPlottable, PlotGen.SERIALIZER_OPTIONS ),
                config.PlotOutputDir );
            links.AppendLine( $"- [Line Counts]({relPath( lineCounter.MarkdownOutputPath )})" );
            readme.AppendLine( "\n## Line Counts\n\n" );
            readme.AppendLine( config.GetMarkdownChartLink( FileLineCounter.LINE_COUNT_HISTORY_TOTAL_FILENAME ) );


            var buildLogAnalyzer = new BuildLogAnalyzer( config );
            buildLogAnalyzer.Analyze();
            PlotGen.CreatePlot(
                JsonSerializer.Serialize( buildLogAnalyzer.GetPlottable(), PlotGen.SERIALIZER_OPTIONS ),
                config.PlotOutputDir );
            links.AppendLine( $"- [Build Warnings]({relPath( buildLogAnalyzer.MarkdownPath )})" );
            readme.AppendLine( "\n## Build Warnings\n\n" );
            readme.AppendLine( config.GetMarkdownChartLink( BuildLogAnalyzer.BUILD_WARNINGS_TOTAL_CHART_FILE_NAME ) );
        }

        links.Insert( 0, "# README\n\n" );
        links.AppendLine();
        links.Append( readme );
        File.WriteAllText(
            Path.Combine( config.OutputDir, "README.md" ),
            links.ToString()
        );

        string relPath( string dest ) =>
            Path.GetRelativePath( config.OutputDir, dest );
    }
}