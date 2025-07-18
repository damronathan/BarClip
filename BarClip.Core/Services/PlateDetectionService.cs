﻿using BarClip.Data.Schema;
using BarClip.Models.Domain;
using Microsoft.ML.OnnxRuntime;

namespace BarClip.Core.Services;

public class PlateDetectionService
{
    public static List<PlateDetection> GetDetections(Frame frame, InferenceSession session)
    {
        return RunInference(frame.InputValue, session);
    }
    private static List<PlateDetection> RunInference(NamedOnnxValue input, InferenceSession session)
    {
        const float ConfidenceThreshold = 0.8f;

        var plateDetections = new List<PlateDetection>();
        var filteredDetections = new List<(int index, float confidence, float xValue)>();

        using var outputs = session.Run([input]);
        var outputTensor = outputs[0].AsTensor<float>();

        int numDetections = outputTensor.Dimensions[2];
        for (int i = 0; i < numDetections; i++)
        {
            float confidence = outputTensor[0, 4, i];
            float xValue = outputTensor[0, 0, i];

            if (confidence > ConfidenceThreshold)
            {
                filteredDetections.Add((i, confidence, xValue));
            }
        }

        var groupedDetections = new List<List<(int index, float confidence, float xValue)>>();

        foreach (var detection in filteredDetections)
        {
            var existingGroup = groupedDetections.FirstOrDefault(
                group => group.Any(d => Math.Abs(d.xValue - detection.xValue) < 5f)
            );

            if (existingGroup != null)
            {
                existingGroup.Add(detection);
            }
            else
            {
                groupedDetections.Add(new List<(int, float, float)> { detection });
            }
        }

        foreach (var group in groupedDetections)
        {
            var bestDetection = group.OrderByDescending(d => d.confidence).First();

            var plateDetection = new PlateDetection
            {
                X = outputTensor[0, 0, bestDetection.index],
                Y = outputTensor[0, 1, bestDetection.index],
                Width = outputTensor[0, 2, bestDetection.index],
                Height = outputTensor[0, 3, bestDetection.index],
                Confidence = bestDetection.confidence,
                DetectionNumber = bestDetection.index

            };

            plateDetections.Add(plateDetection);
        }


        return plateDetections;

    }

}
