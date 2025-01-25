using FileFlows.Shared.Models;

namespace FileFlows.Shared.Helpers;

/// <summary>
/// Helper to build a flow in code
/// </summary>
public class FlowBuilder
{
    private const int ROW_OFFSET = -60;
    private const int COL_OFFSET = -20;
    private const int COL_WIDTH = 100;
    private const int ROW_HEIGHT = 140;
    private const int MAX_ROWS = 7;
    private int CurrentColumn = 1, CurrentRow = 1;

    /// <summary>
    /// Initialises a new instance of the Flow Builder
    /// </summary>
    /// <param name="flowName">the name of the flow</param>
    public FlowBuilder(string flowName)
    {
        Flow = new Flow()
        {
            Name = flowName,
        };
    }
    
    /// <summary>
    /// Gets the flow element UIDs
    /// </summary>
    public FlowElementUids ElementUids { get; init; } = new();

    /// <summary>
    /// Gets the flow tha that is being built
    /// </summary>
    public Flow Flow { get; init; }
    
    /// <summary>
    /// Gets the position of a flow element.
    /// Grid is a 10 row by 30 column
    /// </summary>
    /// <param name="row">its row</param>
    /// <param name="column">its column</param>
    /// <returns>the x and y position</returns>
    public (int xPos, int yPos) GetPosition(int row, int column)
        => (column * COL_WIDTH + COL_OFFSET, row * ROW_HEIGHT + ROW_OFFSET); // row column makes more sense, but xpos then ypos makes more sense

    /// <summary>
    /// Adds a new flow part
    /// </summary>
    /// <param name="row">the row to add it</param>
    /// <param name="column">the column to add it</param>
    /// <param name="flowPart">the part being added</param>
    /// <returns>the flow part that was added</returns>
    public FlowPart Add(FlowPart flowPart, int row = 0, int column = 0)
    {
        if (row == 0)
            row = CurrentRow;
        if (column == 0)
            column = CurrentColumn;
        
        if (flowPart.Uid == Guid.Empty)
            flowPart.Uid = Guid.NewGuid();
        (flowPart.xPos, flowPart.yPos) = GetPosition(row, column);
        Flow.Parts ??= [];
        if (Flow.Parts.Any())
            flowPart.Inputs = 1; // not the first, must have an input
        Flow.Parts.Add(flowPart);
        if (++CurrentRow > MAX_ROWS)
        {
            CurrentRow = 1;
            CurrentColumn += 3;
        }
        return flowPart;
    }

    /// <summary>
    /// Adds a new flow part and connects it to the last flow part
    /// </summary>
    /// <param name="flowPart">the part being added</param>
    /// <param name="output">the output from the last flow part</param>
    /// <param name="row">the row to add it</param>
    /// <param name="column">the column to add it</param>
    /// <param name="allOutputs">If all outputs should be connected</param>
    /// <param name="allOutputsIncludingFailure">If all oututs including the failure output should be connected</param>
    /// <returns>the flow part that was added</returns>
    public FlowPart AddAndConnect(FlowPart flowPart, int output = 1, int row = 0, int column = 0, bool allOutputs = false, bool allOutputsIncludingFailure = false)
    {
        var last = Flow.Parts.Last();
        Add(flowPart, row, column);
        if (allOutputs || allOutputsIncludingFailure)
        {
            for(int i=1;i<=last.Outputs;i++)
                Connect(last, flowPart, i);
            if(allOutputsIncludingFailure)
                Connect(last, flowPart, -1);
        }
        else
        {
            Connect(last, flowPart, output);
        }

        return flowPart;
    }
    /// <summary>
    /// Connect two flow parts together
    /// </summary>
    /// <param name="source">the source flow part</param>
    /// <param name="destination">the destination flow part</param>
    /// <param name="output">the output from the source</param>
    /// <returns>the source flow part</returns>
    public FlowPart Connect(FlowPart source, FlowPart destination, int output)
    {
        source.OutputConnections ??= [];
        source.OutputConnections.Add(new ()
        {
            Input = 1,
            Output = output,
            InputNode = destination.Uid
        });
        return source;
    }
}

/// <summary>
/// Common flow element uids to be used by flow parts
/// </summary>
public class FlowElementUids
{
    /// <summary>
    /// Gets the UID for a Replace Original flow element
    /// </summary>
    public string ReplaceOriginal => "FileFlows.BasicNodes.File.ReplaceOriginal";
    /// <summary>
    /// Gets the UID for a Move File flow element
    /// </summary>
    public string MoveFile => "FileFlows.BasicNodes.File.MoveFile";
    /// <summary>
    /// Gets the UID for a Delete Source Directory flow element
    /// </summary>
    public string DeleteSourceDirectory => "FileFlows.BasicNodes.File.DeleteSourceDirectory";
    
    /// <summary>
    /// Gets the UID for a video file flow element
    /// </summary>
    public string VideoFile => "FileFlows.VideoNodes.VideoFile";

    /// <summary>
    /// Gets the UID for FFmpeg Builder Start
    /// </summary>
    public string FFmpegBuilderStart => "FileFlows.VideoNodes.FfmpegBuilderNodes.FfmpegBuilderStart";

    /// <summary>
    /// Gets the UID for FFmpeg Builder Executor
    /// </summary>
    public string FFmpegBuilderExecutor => "FileFlows.VideoNodes.FfmpegBuilderNodes.FfmpegBuilderExecutor";

    /// <summary>
    /// Gets the UID for FFmpeg Builder Remux to MKV
    /// </summary>
    public string FFmpegBuilderRemuxToMkv => "FileFlows.VideoNodes.FfmpegBuilderNodes.FfmpegBuilderRemuxToMkv";

    /// <summary>
    /// Gets the UID for FFmpeg Builder Remux to MP4
    /// </summary>
    public string FFmpegBuilderRemuxToMp4 => "FileFlows.VideoNodes.FfmpegBuilderNodes.FfmpegBuilderRemuxToMp4";

    /// <summary>
    /// Gets the UID for FFmpeg Builder Crop Black Bars
    /// </summary>
    public string FFmpegBuildeCropBlackBars => "FileFlows.VideoNodes.FfmpegBuilderNodes.FfmpegBuilderCropBlackBars";

    /// <summary>
    /// Gets the UID for FFmpeg Builder Video Encode
    /// </summary>
    public string FFmpegBuildeVideoEncode => "FileFlows.VideoNodes.FfmpegBuilderNodes.FfmpegBuilderVideoEncode";

    /// <summary>
    /// Gets the UID for FFmpeg Builder Audio Language Converter
    /// </summary>
    public string FFmpegBuilderAudioLanguageConverter => "FileFlows.VideoNodes.FfmpegBuilderNodes.FfmpegBuilderAudioLanguageConverter";

    /// <summary>
    /// Gets the UID for FFmpeg Builder Set Language
    /// </summary>
    public string FFmpegBuilderAudioSetLanguage => "FileFlows.VideoNodes.FfmpegBuilderNodes.FfmpegBuilderAudioSetLanguage";

    /// <summary>
    /// Gets the UID for FFmpeg Builder Video Encode (Quality Encoding)
    /// </summary>
    public string FFmpegBuilderVideoEncode => "FileFlows.VideoNodes.FfmpegBuilderNodes.FfmpegBuilderVideoEncode";

    /// <summary>
    /// Gets the UID for FFmpeg Builder Bitrate Encode
    /// </summary>
    public string FFmpegBuilderVideoBitrateEncode => "FileFlows.VideoNodes.FfmpegBuilderNodes.FfmpegBuilderVideoBitrateEncode";

    /// <summary>
    /// Gets the UID for FFmpeg Builder Track Remover
    /// </summary>
    public string FFmpegBuilderTrackRemover => "FileFlows.VideoNodes.FfmpegBuilderNodes.FfmpegBuilderTrackRemover";

    /// <summary>
    /// Gets the UID for FFmpeg Builder Language Remover
    /// </summary>
    public string FFmpegBuilderLanguageRemover => "FileFlows.VideoNodes.FfmpegBuilderNodes.FFmpegBuilderLanguageRemover";
    
    
    /// <summary>
    /// Gets the UID for Movie Lookup 
    /// </summary>
    public string MovieLookup => "MetaNodes.TheMovieDb.MovieLookup";
    
    /// <summary>
    /// Gets the UID for TV Show Lookup 
    /// </summary>
    public string TVShowLookup => "MetaNodes.TheMovieDb.TVShowLookup";
}