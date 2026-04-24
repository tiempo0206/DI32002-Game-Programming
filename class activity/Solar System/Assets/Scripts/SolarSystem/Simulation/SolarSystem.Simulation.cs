using UnityEngine;

public sealed partial class SolarSystem
{
    private void UpdatePlanets()
    {
        for (int i = 0; i < bodies.Count; i++)
        {
            OrbitBody body = bodies[i];
            PlanetDefinition definition = body.Definition;
            float anomaly = definition.InitialMeanAnomalyDegrees + (simulatedDays / definition.OrbitalPeriodDays) * 360f;
            body.OrbitingRoot.localPosition = ComputeOrbitPosition(definition.SemiMajorAxisAU, definition.Eccentricity, definition.InclinationDegrees, anomaly);

            float spinAngle = CalculateSpinAngle(definition.DayLengthHours);
            body.AxisRoot.localRotation = Quaternion.AngleAxis(definition.AxialTiltDegrees, Vector3.forward) * Quaternion.AngleAxis(spinAngle, Vector3.up);

            if (body.MoonOrbitingRoot != null)
            {
                float moonAnomaly = 125f + (simulatedDays / MoonOrbitDays) * 360f;
                body.MoonOrbitingRoot.localPosition = ComputeMoonPosition(moonAnomaly, 5.14f);
                body.MoonAxisRoot.localRotation = Quaternion.AngleAxis(6.68f, Vector3.forward) * Quaternion.AngleAxis(CalculateSpinAngle(MoonDayHours), Vector3.up);
            }
        }
    }

    private void UpdateAsteroids()
    {
        for (int i = 0; i < asteroids.Count; i++)
        {
            AsteroidBody asteroid = asteroids[i];
            float anomaly = asteroid.InitialMeanAnomalyDegrees + (simulatedDays / asteroid.OrbitalPeriodDays) * 360f;
            asteroid.Transform.localPosition = ComputeOrbitPosition(asteroid.SemiMajorAxisAU, asteroid.Eccentricity, asteroid.InclinationDegrees, anomaly);
            asteroid.Transform.Rotate(Vector3.up, Time.deltaTime * 16f, Space.Self);
        }
    }

    private void FaceLabelsToCamera()
    {
        if (sceneCamera == null)
        {
            sceneCamera = Camera.main;
        }

        if (sceneCamera == null)
        {
            return;
        }

        for (int i = 0; i < bodies.Count; i++)
        {
            FaceLabel(bodies[i].Label);
            FaceLabel(bodies[i].MoonLabel);
        }
    }

    private void FaceLabel(Transform label)
    {
        if (label == null || !label.gameObject.activeSelf)
        {
            return;
        }

        Vector3 direction = label.position - sceneCamera.transform.position;
        if (direction.sqrMagnitude > 0.001f)
        {
            label.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }
    }

    private Vector3 ComputeMoonPosition(float meanAnomalyDegrees, float inclinationDegrees)
    {
        float angle = meanAnomalyDegrees * Mathf.Deg2Rad;
        Vector3 local = new Vector3(Mathf.Cos(angle) * earthMoonOrbitRadius, 0f, Mathf.Sin(angle) * earthMoonOrbitRadius);
        return Quaternion.AngleAxis(inclinationDegrees, Vector3.right) * local;
    }

    private Vector3 ComputeOrbitPosition(float semiMajorAxisAU, float eccentricity, float inclinationDegrees, float meanAnomalyDegrees)
    {
        float a = semiMajorAxisAU * unitsPerAU;
        float e = Mathf.Clamp(eccentricity, 0f, 0.9f);
        float b = a * Mathf.Sqrt(1f - e * e);
        float angle = meanAnomalyDegrees * Mathf.Deg2Rad;
        Vector3 local = new Vector3(a * (Mathf.Cos(angle) - e), 0f, b * Mathf.Sin(angle));
        return Quaternion.AngleAxis(inclinationDegrees, Vector3.right) * local;
    }

    private float CalculateSpinAngle(float dayLengthHours)
    {
        if (Mathf.Abs(dayLengthHours) < 0.001f)
        {
            return 0f;
        }

        return (simulatedDays * 24f / dayLengthHours) * 360f;
    }

    private void HandleKeyboardInput()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            paused = !paused;
        }

        if (Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.KeypadPlus))
        {
            daysPerSecond *= 2f;
        }

        if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus))
        {
            daysPerSecond = Mathf.Max(0.125f, daysPerSecond * 0.5f);
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            simulatedDays = 0f;
        }

        if (Input.GetKeyDown(KeyCode.O))
        {
            showOrbitLines = !showOrbitLines;
            for (int i = 0; i < orbitLineObjects.Count; i++)
            {
                orbitLineObjects[i].SetActive(showOrbitLines);
            }
        }

        if (Input.GetKeyDown(KeyCode.L))
        {
            showLabels = !showLabels;
            for (int i = 0; i < bodies.Count; i++)
            {
                SetLabelVisible(bodies[i].Label, showLabels);
                SetLabelVisible(bodies[i].MoonLabel, showLabels);
            }
        }
    }

    private void SetLabelVisible(Transform label, bool visible)
    {
        if (label != null)
        {
            label.gameObject.SetActive(visible);
        }
    }
}
