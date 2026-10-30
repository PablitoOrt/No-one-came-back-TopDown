using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class FirearmEffectsPresetTool
{
    private const string RootFolder = "Assets/Data/Weapons/Effects";
    private const string DefaultParticleMaterialPath = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/ParticlesUnlit.mat";

    private struct Preset
    {
        public string Name;
        public int FlashCount;
        public Vector2 FlashSize;
        public float FlashLifetime;
        public float FlashSpeed;
        public float FlashAngle;
        public Color FlashColor;
        public float LightIntensity;
        public float LightRange;
        public float LightDuration;
        public int SmokeCount;
        public int FlashPelletCount;
        public float FlashPelletAngle;
        public Vector2 FlashPelletSpeed;
        public Vector3 ShellSize;
        public Color ShellColor;
        public float ShellSpeed;
        public float TracerSpeed;
        public float TracerWidth;
        public Color TracerColor;
        public int SparkCount;
        public int DustCount;
        public int PoolSize;
    }

    private static readonly Preset Pistol = new Preset
    {
        Name = "Pistol",
        FlashCount = 6,
        FlashSize = new Vector2(0.2f, 0.35f),
        FlashLifetime = 0.05f,
        FlashSpeed = 2f,
        FlashAngle = 18f,
        FlashColor = new Color(4f, 2.2f, 0.8f, 1f),
        LightIntensity = 3f,
        LightRange = 4f,
        LightDuration = 0.05f,
        SmokeCount = 3,
        ShellSize = new Vector3(0.012f, 0.012f, 0.025f),
        ShellColor = new Color(0.85f, 0.65f, 0.25f, 1f),
        ShellSpeed = 2.2f,
        TracerSpeed = 140f,
        TracerWidth = 0.03f,
        TracerColor = new Color(4f, 3f, 1.2f, 1f),
        SparkCount = 8,
        DustCount = 3,
        PoolSize = 4,
    };

    private static readonly Preset Rifle = new Preset
    {
        Name = "Rifle",
        FlashCount = 4,
        FlashSize = new Vector2(0.15f, 0.28f),
        FlashLifetime = 0.035f,
        FlashSpeed = 2.5f,
        FlashAngle = 12f,
        FlashColor = new Color(5f, 2.6f, 0.9f, 1f),
        LightIntensity = 2.5f,
        LightRange = 3.5f,
        LightDuration = 0.04f,
        SmokeCount = 1,
        ShellSize = new Vector3(0.01f, 0.01f, 0.03f),
        ShellColor = new Color(0.85f, 0.65f, 0.25f, 1f),
        ShellSpeed = 2.8f,
        TracerSpeed = 180f,
        TracerWidth = 0.025f,
        TracerColor = new Color(5f, 3.5f, 1.2f, 1f),
        SparkCount = 6,
        DustCount = 2,
        PoolSize = 8,
    };

    private static readonly Preset Shotgun = new Preset
    {
        Name = "Shotgun",
        FlashCount = 14,
        FlashSize = new Vector2(0.35f, 0.6f),
        FlashLifetime = 0.08f,
        FlashSpeed = 3.5f,
        FlashAngle = 28f,
        FlashColor = new Color(4f, 1.8f, 0.6f, 1f),
        LightIntensity = 5f,
        LightRange = 6f,
        LightDuration = 0.08f,
        SmokeCount = 6,
        FlashPelletCount = 8,
        FlashPelletAngle = 8f,
        FlashPelletSpeed = new Vector2(35f, 55f),
        ShellSize = new Vector3(0.02f, 0.02f, 0.045f),
        ShellColor = new Color(0.6f, 0.08f, 0.06f, 1f),
        ShellSpeed = 1.6f,
        TracerSpeed = 110f,
        TracerWidth = 0.02f,
        TracerColor = new Color(3f, 2.2f, 1f, 1f),
        SparkCount = 3,
        DustCount = 1,
        PoolSize = 3,
    };

    [MenuItem("Tools/Weapons/Create Default Shot Effects")]
    private static void CreateDefaultShotEffects()
    {
        EnsureFolder(RootFolder);

        Material smokeMaterial = LoadDefaultParticleMaterial();
        Material additiveMaterial = GetOrCreateAdditiveMaterial($"{RootFolder}/ParticlesAdditive.mat", smokeMaterial);
        Material shellMaterial = GetOrCreateShellMaterial($"{RootFolder}/Shell.mat");

        var profiles = new Dictionary<string, FirearmEffectsProfile>();
        foreach (Preset preset in new[] { Pistol, Rifle, Shotgun })
        {
            profiles[preset.Name] = BuildPreset(preset, additiveMaterial, smokeMaterial, shellMaterial);
        }

        var assigned = new List<string>();
        foreach (string guid in AssetDatabase.FindAssets("t:FirearmDefinition"))
        {
            FirearmDefinition definition = AssetDatabase.LoadAssetAtPath<FirearmDefinition>(AssetDatabase.GUIDToAssetPath(guid));
            if (definition == null || definition.EffectsProfile != null) continue;

            string presetName = definition.PelletsPerShot > 1 ? Shotgun.Name
                : definition.FireMode == WeaponFireMode.Automatic ? Rifle.Name
                : Pistol.Name;

            var serialized = new SerializedObject(definition);
            serialized.FindProperty("effectsProfile").objectReferenceValue = profiles[presetName];
            serialized.ApplyModifiedPropertiesWithoutUndo();
            assigned.Add($"{definition.name} → {presetName}");
        }

        AssetDatabase.SaveAssets();

        string summary = assigned.Count > 0 ? string.Join("\n", assigned) : "No firearm without an effects profile was found.";
        EditorUtility.DisplayDialog("Shot Effects", $"Presets created in {RootFolder}.\n\n{summary}", "OK");
    }

    private static FirearmEffectsProfile BuildPreset(Preset preset, Material additive, Material smoke, Material shell)
    {
        string folder = $"{RootFolder}/{preset.Name}";
        EnsureFolder(folder);

        ParticleSystem muzzleFlash = SavePrefab(BuildMuzzleFlash(preset, additive, smoke), $"{folder}/{preset.Name}_MuzzleFlash.prefab");
        ParticleSystem shellEject = SavePrefab(BuildShells(preset, shell), $"{folder}/{preset.Name}_Shells.prefab");
        ParticleSystem tracer = SavePrefab(BuildTracer(preset, additive), $"{folder}/{preset.Name}_Tracer.prefab");
        ParticleSystem impact = SavePrefab(BuildImpact(preset, additive, smoke), $"{folder}/{preset.Name}_Impact.prefab");

        string profilePath = $"{folder}/{preset.Name}_Effects.asset";
        FirearmEffectsProfile profile = AssetDatabase.LoadAssetAtPath<FirearmEffectsProfile>(profilePath);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<FirearmEffectsProfile>();
            AssetDatabase.CreateAsset(profile, profilePath);
        }

        var serialized = new SerializedObject(profile);
        serialized.FindProperty("muzzleFlash").objectReferenceValue = muzzleFlash;
        serialized.FindProperty("useMuzzleLight").boolValue = true;
        serialized.FindProperty("muzzleLightIntensity").floatValue = preset.LightIntensity;
        serialized.FindProperty("muzzleLightRange").floatValue = preset.LightRange;
        serialized.FindProperty("muzzleLightDuration").floatValue = preset.LightDuration;
        serialized.FindProperty("shellEject").objectReferenceValue = shellEject;
        serialized.FindProperty("tracer").objectReferenceValue = tracer;
        serialized.FindProperty("tracerSpeed").floatValue = preset.TracerSpeed;
        serialized.FindProperty("impact").objectReferenceValue = impact;
        serialized.FindProperty("poolSize").intValue = preset.PoolSize;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(profile);

        return profile;
    }

    private static GameObject BuildMuzzleFlash(Preset preset, Material additive, Material smoke)
    {
        ParticleSystem flash = NewSystem($"{preset.Name}_MuzzleFlash", null, additive, 0.2f);
        ParticleSystem.MainModule main = flash.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(preset.FlashLifetime * 0.6f, preset.FlashLifetime);
        main.startSpeed = new ParticleSystem.MinMaxCurve(preset.FlashSpeed * 0.5f, preset.FlashSpeed);
        main.startSize = new ParticleSystem.MinMaxCurve(preset.FlashSize.x, preset.FlashSize.y);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = preset.FlashColor;
        Burst(flash, preset.FlashCount);
        Cone(flash, preset.FlashAngle, 0.01f);
        SizeOverLifetime(flash, AnimationCurve.Linear(0f, 1f, 1f, 0.3f));
        FadeOut(flash);

        if (preset.SmokeCount > 0)
        {
            ParticleSystem puff = NewSystem("Smoke", flash.transform, smoke, 1f);
            ParticleSystem.MainModule puffMain = puff.main;
            puffMain.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1f);
            puffMain.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 0.8f);
            puffMain.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.3f);
            puffMain.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            puffMain.startColor = new Color(0.6f, 0.6f, 0.6f, 0.35f);
            Burst(puff, preset.SmokeCount);
            Cone(puff, 20f, 0.02f);
            SizeOverLifetime(puff, AnimationCurve.Linear(0f, 1f, 1f, 2.5f));
            FadeOut(puff);
        }

        if (preset.FlashPelletCount > 0)
        {
            ParticleSystem pellets = NewSystem("Pellets", flash.transform, additive, 0.2f);
            ParticleSystem.MainModule pelletMain = pellets.main;
            pelletMain.startLifetime = new ParticleSystem.MinMaxCurve(0.05f, 0.09f);
            pelletMain.startSpeed = new ParticleSystem.MinMaxCurve(preset.FlashPelletSpeed.x, preset.FlashPelletSpeed.y);
            pelletMain.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.035f);
            pelletMain.startColor = new Color(5f, 3.2f, 1.2f, 1f);
            Burst(pellets, preset.FlashPelletCount);
            Cone(pellets, preset.FlashPelletAngle, 0.015f);
            FadeOut(pellets);

            ParticleSystemRenderer pelletRenderer = pellets.GetComponent<ParticleSystemRenderer>();
            pelletRenderer.renderMode = ParticleSystemRenderMode.Stretch;
            pelletRenderer.velocityScale = 0.025f;
            pelletRenderer.lengthScale = 1f;

            ParticleSystem.MainModule flashMain = flash.main;
            flashMain.startSpeed = new ParticleSystem.MinMaxCurve(preset.FlashSpeed * 0.3f, preset.FlashSpeed * 0.6f);
        }

        return flash.gameObject;
    }

    private static GameObject BuildShells(Preset preset, Material shell)
    {
        ParticleSystem shells = NewSystem($"{preset.Name}_Shells", null, shell, 0.1f);
        ParticleSystem.MainModule main = shells.main;
        main.startLifetime = 2f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(preset.ShellSpeed * 0.8f, preset.ShellSpeed * 1.2f);
        main.startSize3D = true;
        main.startSizeX = preset.ShellSize.x;
        main.startSizeY = preset.ShellSize.y;
        main.startSizeZ = preset.ShellSize.z;
        main.startRotation3D = true;
        main.startRotationX = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startRotationY = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startRotationZ = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = preset.ShellColor;
        main.gravityModifier = 1f;
        Burst(shells, 1);

        ParticleSystem.ShapeModule shape = shells.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 12f;
        shape.radius = 0.005f;
        shape.rotation = new Vector3(-35f, 90f, 0f);

        ParticleSystem.RotationOverLifetimeModule spin = shells.rotationOverLifetime;
        spin.enabled = true;
        spin.separateAxes = true;
        spin.x = new ParticleSystem.MinMaxCurve(-12f, 12f);
        spin.y = new ParticleSystem.MinMaxCurve(-12f, 12f);
        spin.z = new ParticleSystem.MinMaxCurve(-12f, 12f);

        ParticleSystem.CollisionModule collision = shells.collision;
        collision.enabled = true;
        collision.type = ParticleSystemCollisionType.World;
        collision.mode = ParticleSystemCollisionMode.Collision3D;
        collision.bounce = 0.35f;
        collision.dampen = 0.3f;
        collision.lifetimeLoss = 0f;

        SizeOverLifetime(shells, new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.85f, 1f), new Keyframe(1f, 0f)));

        ParticleSystemRenderer renderer = shells.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Mesh;
        renderer.mesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
        renderer.alignment = ParticleSystemRenderSpace.World;
        renderer.shadowCastingMode = ShadowCastingMode.On;

        return shells.gameObject;
    }

    private static GameObject BuildTracer(Preset preset, Material additive)
    {
        ParticleSystem tracer = NewSystem($"{preset.Name}_Tracer", null, additive, 1f);
        ParticleSystem.MainModule main = tracer.main;
        main.startLifetime = 1f;
        main.startSpeed = 0f;
        main.startSize = preset.TracerWidth;
        main.startColor = preset.TracerColor;
        main.maxParticles = 128;

        ParticleSystem.EmissionModule emission = tracer.emission;
        emission.enabled = false;
        ParticleSystem.ShapeModule shape = tracer.shape;
        shape.enabled = false;

        ParticleSystemRenderer renderer = tracer.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.velocityScale = 0.02f;
        renderer.lengthScale = 1f;

        return tracer.gameObject;
    }

    private static GameObject BuildImpact(Preset preset, Material additive, Material smoke)
    {
        ParticleSystem sparks = NewSystem($"{preset.Name}_Impact", null, additive, 0.5f);
        ParticleSystem.MainModule main = sparks.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.15f, 0.35f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 6f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.015f, 0.035f);
        main.startColor = new Color(4f, 2f, 0.6f, 1f);
        main.gravityModifier = 1f;
        Burst(sparks, preset.SparkCount);
        Cone(sparks, 40f, 0.02f);
        FadeOut(sparks);

        ParticleSystemRenderer renderer = sparks.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.velocityScale = 0.04f;
        renderer.lengthScale = 1f;

        if (preset.DustCount > 0)
        {
            ParticleSystem dust = NewSystem("Dust", sparks.transform, smoke, 1f);
            ParticleSystem.MainModule dustMain = dust.main;
            dustMain.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.7f);
            dustMain.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 1f);
            dustMain.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.3f);
            dustMain.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            dustMain.startColor = new Color(0.5f, 0.47f, 0.42f, 0.5f);
            Burst(dust, preset.DustCount);
            Cone(dust, 30f, 0.03f);
            SizeOverLifetime(dust, AnimationCurve.Linear(0f, 1f, 1f, 2f));
            FadeOut(dust);
        }

        return sparks.gameObject;
    }

    private static ParticleSystem NewSystem(string name, Transform parent, Material material, float duration)
    {
        var go = new GameObject(name);
        if (parent != null) go.transform.SetParent(parent, false);

        ParticleSystem system = go.AddComponent<ParticleSystem>();
        system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = system.main;
        main.duration = duration;
        main.loop = false;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        ParticleSystem.EmissionModule emission = system.emission;
        emission.rateOverTime = 0f;

        go.GetComponent<ParticleSystemRenderer>().sharedMaterial = material;
        return system;
    }

    private static void Burst(ParticleSystem system, int count)
    {
        ParticleSystem.EmissionModule emission = system.emission;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
    }

    private static void Cone(ParticleSystem system, float angle, float radius)
    {
        ParticleSystem.ShapeModule shape = system.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = angle;
        shape.radius = radius;
    }

    private static void SizeOverLifetime(ParticleSystem system, AnimationCurve curve)
    {
        ParticleSystem.SizeOverLifetimeModule size = system.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, curve);
    }

    private static void FadeOut(ParticleSystem system)
    {
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });

        ParticleSystem.ColorOverLifetimeModule color = system.colorOverLifetime;
        color.enabled = true;
        color.color = gradient;
    }

    private static ParticleSystem SavePrefab(GameObject source, string path)
    {
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(source, path);
        Object.DestroyImmediate(source);
        return prefab.GetComponent<ParticleSystem>();
    }

    private static Material LoadDefaultParticleMaterial()
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(DefaultParticleMaterialPath);
        if (material != null) return material;

        return new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
    }

    private static Material GetOrCreateAdditiveMaterial(string path, Material template)
    {
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;

        var material = new Material(template) { name = "ParticlesAdditive" };
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", 2f);
        material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", (float)BlendMode.One);
        material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
        material.SetFloat("_DstBlendAlpha", (float)BlendMode.One);
        material.SetFloat("_ZWrite", 0f);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.DisableKeyword("_ALPHAMODULATE_ON");
        material.renderQueue = (int)RenderQueue.Transparent;

        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static Material GetOrCreateShellMaterial(string path)
    {
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;

        var material = new Material(Shader.Find("Universal Render Pipeline/Particles/Simple Lit")) { name = "Shell" };
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;

        int slash = path.LastIndexOf('/');
        string parent = path.Substring(0, slash);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, path.Substring(slash + 1));
    }
}
