using BarClip.Models.Domain;
using BarClip.Models.Requests;
using Microsoft.Identity.Client;

namespace BarClip.Core.Services;
public class PlateAnalysisService
{
    public void SetTrim(OriginalVideoRequest video)
    {
        int frameNumber = GetStartFrame(video);
        video.TrimStart = TimeSpan.FromSeconds(frameNumber - .5);
        video.TrimFinish = GetTrimFinish(frameNumber, video);
    }


    public int GetStartFrame(OriginalVideoRequest video)
    {
        float yValue = 0f;
        bool initialYFound = false;
        int frameNumber = 0;
        PlateDetection previousDetection = null;

        foreach (Frame frame in video.Frames)
        {
            if (frame.PlateDetections.Count > 0)
            {
                int x = 1;
                foreach (var detection in frame.PlateDetections)
                {
                    x++;
                }
                PlateDetection plateDetection = SelectBestDetection(frame, previousDetection);
                if (plateDetection == null) continue;

                if (!initialYFound)
                {
                    yValue = plateDetection.Y;
                    initialYFound = true;
                }

                if (initialYFound && Math.Abs(plateDetection.Y - yValue) > plateDetection.Height / 2)
                {
                    frameNumber = frame.FrameNumber;
                    break;
                }

                previousDetection = plateDetection;
            }
        }

        return Math.Max(frameNumber - 1, 1);
    }

    public TimeSpan GetTrimFinish(int trim, OriginalVideoRequest video)
    {
        PlateDetection? previousDetection = null;
        bool lastDetectionIsCurrent = false;


        //Loop starting from the end of the video
        for (int i = video.Frames.Count - 1; i >= trim; i--)
        {
            Frame frame = video.Frames[i];

            if (frame.PlateDetections.Count > 0)
            {

                (PlateDetection currentDetection, lastDetectionIsCurrent) = SelectBestDetection(frame, previousDetection, lastDetectionIsCurrent);

                //Only null if no detection found yet
                if (currentDetection is null)
                {
                    continue;
                }


                if (previousDetection is not null)
                {

                    //If plate has moved up or down
                    if (Math.Abs(currentDetection.Y - previousDetection.Y) > currentDetection.Height / 2)
                    {
                        if (lastDetectionIsCurrent is false)
                        {
                            double endFrame = frame.FrameNumber + 1.5;
                            return TimeSpan.FromSeconds(endFrame);
                            //If this is past the second detection, create the trim point
                        }
                        else
                        {
                            //If first 2 detections have vertical movement, return whole video.
                            return video.Duration;
                        }
                    }
                }

                previousDetection = currentDetection;
            }
        }

        return video.Duration;
    }

    public PlateDetection SelectBestDetection(Frame frame, PlateDetection referenceDetection)
    {
        var (detection, _) = SelectBestDetection(frame, referenceDetection, false);
        return detection;
    }
    public static (PlateDetection, bool) SelectBestDetection(Frame frame, PlateDetection referenceDetection, bool lastDetectionIsCurrent)
    {
        //If no detections in frame, return null or reference detection
        if (frame.PlateDetections is null)
        return (referenceDetection, lastDetectionIsCurrent);

        //If it is the first frame, or no detections yet, reference detection will be null
        //Return the best detection from the frame, this is the last detection
        if (referenceDetection is null)
        {
            var lastDetection = frame.PlateDetections.OrderByDescending(pd => pd.Confidence).FirstOrDefault();
            if (lastDetection == null) return (referenceDetection, lastDetectionIsCurrent);
            return (lastDetection, true);
        }

        //Filters out the second plate
        var candidateDetections = frame.PlateDetections
        .Where(pd => Math.Abs(pd.X - referenceDetection.X) < 50)
        .ToList();

        //Selects the current plate. Checks to make sure the second isn't close enough to make the list
        if (candidateDetections.Any())
        {
            var currentDetection = candidateDetections.OrderBy(pd => Math.Abs(pd.X - referenceDetection.X)).First();
            return (currentDetection, false);
        }

        //Allows second plate to be selected if close enough y value to first plate after 5th frame
        //The y check is to prevent false movement triggers
        //sometimes first plate will be obstructed and second plate is valid to check height change.
        if (frame.FrameNumber > 10)
        {
            var closestByX = frame.PlateDetections.OrderBy(pd => Math.Abs(pd.X - referenceDetection.X)).FirstOrDefault();
            if (closestByX == null) return (referenceDetection, false);

            if (Math.Abs(closestByX.Height - referenceDetection.Height) < 20)
            {
                return (closestByX, false);
            }
            else
            {
                return (referenceDetection, false);
            }
        }
        return (referenceDetection, false);
    }
}
