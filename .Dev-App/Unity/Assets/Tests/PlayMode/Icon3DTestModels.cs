using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
#endif

namespace GuiToolkit.Test
{
	/// <summary>
	/// Procedural, self made test models for the 3D icons (no third party assets): a skinned tentacle and a skinned
	/// puppet, both with an Animator, a goblet with a transparent bowl, and a trophy with three materials. Built-in
	/// shaders only, so they run in any pipeline the dev project uses.
	/// The geometry is built by code; in the editor <see cref="EnsureAssets"/> saves everything as real assets
	/// (meshes, materials, clips, controllers, prefabs) in <see cref="Folder"/>, from the menu or on demand in edit mode.
	/// </summary>
	public static class Icon3DTestModels
	{
		public const string Folder = "Assets/Tests/Icon3D/TestModels";
		public const string TentaclePath = Folder + "/TestTentacle.prefab";
		public const string PuppetPath = Folder + "/TestPuppet.prefab";
		public const string GobletPath = Folder + "/TestGoblet.prefab";
		public const string TrophyPath = Folder + "/TestTrophy.prefab";

		public const int TentacleBones = 5;
		public const int PuppetBones = 9;

		#region Geometry

		private struct Part
		{
			public Mesh Mesh;
			public Matrix4x4 Matrix;   // part space -> model space
			public int Bone;
		}

		/// A tube along +Y with <paramref name="_boneCount"/> bones, smoothly weighted between neighbouring bones.
		public static Mesh BuildTentacleMesh( int _boneCount, float _length, float _radius, out Matrix4x4[] _bindPoses, out Vector3[] _bonePositions )
		{
			const int sides = 14;
			const int ringsPerBone = 5;
			int rings = (_boneCount - 1) * ringsPerBone + 1;

			var vertices = new List<Vector3>();
			var uvs = new List<Vector2>();
			var weights = new List<BoneWeight>();
			for (int ring = 0; ring < rings; ring++)
			{
				float t = ring / (float)(rings - 1);
				float y = t * _length;
				float radius = Mathf.Lerp(_radius, _radius * 0.2f, t);   // tapering
				float boneFloat = t * (_boneCount - 1);
				int lower = Mathf.Min((int)boneFloat, _boneCount - 2);
				float blend = boneFloat - lower;
				var weight = new BoneWeight
				{
					boneIndex0 = lower,
					boneIndex1 = lower + 1,
					weight0 = 1 - blend,
					weight1 = blend,
				};
				for (int side = 0; side <= sides; side++)
				{
					float angle = side / (float)sides * Mathf.PI * 2;
					vertices.Add(new Vector3(Mathf.Cos(angle) * radius, y, Mathf.Sin(angle) * radius));
					uvs.Add(new Vector2(side / (float)sides, t));
					weights.Add(weight);
				}
			}

			var triangles = new List<int>();
			int stride = sides + 1;
			for (int ring = 0; ring < rings - 1; ring++)
			{
				for (int side = 0; side < sides; side++)
				{
					int a = ring * stride + side;
					int b = a + stride;
					triangles.AddRange(new[] { a, b, a + 1, a + 1, b, b + 1 });
				}
			}

			// Cap on top
			int tip = vertices.Count;
			vertices.Add(new Vector3(0, _length, 0));
			uvs.Add(new Vector2(0.5f, 1));
			weights.Add(new BoneWeight { boneIndex0 = _boneCount - 1, weight0 = 1 });
			int lastRing = (rings - 1) * stride;
			for (int side = 0; side < sides; side++)
				triangles.AddRange(new[] { lastRing + side, tip, lastRing + side + 1 });

			var mesh = new Mesh { name = "TestTentacle" };
			mesh.SetVertices(vertices);
			mesh.SetUVs(0, uvs);
			mesh.SetTriangles(triangles, 0);
			mesh.boneWeights = weights.ToArray();
			mesh.RecalculateNormals();
			mesh.RecalculateBounds();

			_bonePositions = new Vector3[_boneCount];
			_bindPoses = new Matrix4x4[_boneCount];
			for (int i = 0; i < _boneCount; i++)
			{
				_bonePositions[i] = new Vector3(0, i * _length / (_boneCount - 1), 0);
				_bindPoses[i] = Matrix4x4.Translate(-_bonePositions[i]);
			}
			mesh.bindposes = _bindPoses;
			return mesh;
		}

		/// A body of rigidly bound primitives (every part follows exactly one bone).
		private static Mesh BuildRigidMesh( List<Part> _parts, Matrix4x4[] _bindPoses, string _name )
		{
			var vertices = new List<Vector3>();
			var normals = new List<Vector3>();
			var uvs = new List<Vector2>();
			var weights = new List<BoneWeight>();
			var triangles = new List<int>();

			foreach (var part in _parts)
			{
				int offset = vertices.Count;
				var source = part.Mesh;
				var sourceVertices = source.vertices;
				var sourceNormals = source.normals;
				var sourceUvs = source.uv;
				for (int i = 0; i < sourceVertices.Length; i++)
				{
					vertices.Add(part.Matrix.MultiplyPoint3x4(sourceVertices[i]));
					normals.Add(part.Matrix.MultiplyVector(sourceNormals[i]).normalized);
					uvs.Add(sourceUvs[i]);
					weights.Add(new BoneWeight { boneIndex0 = part.Bone, weight0 = 1 });
				}
				foreach (int index in source.triangles)
					triangles.Add(index + offset);
			}

			var mesh = new Mesh { name = _name };
			mesh.SetVertices(vertices);
			mesh.SetNormals(normals);
			mesh.SetUVs(0, uvs);
			mesh.SetTriangles(triangles, 0);
			mesh.boneWeights = weights.ToArray();
			mesh.bindposes = _bindPoses;
			mesh.RecalculateBounds();
			return mesh;
		}

		/// A body of revolution around Y from a profile of (radius, height) points.
		public static Mesh BuildLathe( IList<Vector2> _profile, string _name, int _sides = 24 )
		{
			var vertices = new List<Vector3>();
			var uvs = new List<Vector2>();
			for (int p = 0; p < _profile.Count; p++)
			{
				for (int side = 0; side <= _sides; side++)
				{
					float angle = side / (float)_sides * Mathf.PI * 2;
					vertices.Add(new Vector3(Mathf.Cos(angle) * _profile[p].x, _profile[p].y, Mathf.Sin(angle) * _profile[p].x));
					uvs.Add(new Vector2(side / (float)_sides, p / (float)(_profile.Count - 1)));
				}
			}

			var triangles = new List<int>();
			int stride = _sides + 1;
			for (int p = 0; p < _profile.Count - 1; p++)
			{
				for (int side = 0; side < _sides; side++)
				{
					int a = p * stride + side;
					int b = a + stride;
					triangles.AddRange(new[] { a, a + 1, b, a + 1, b + 1, b });
				}
			}

			var mesh = new Mesh { name = _name };
			mesh.SetVertices(vertices);
			mesh.SetUVs(0, uvs);
			mesh.SetTriangles(triangles, 0);
			mesh.RecalculateNormals();
			mesh.RecalculateBounds();
			return mesh;
		}

		private static Mesh Primitive( PrimitiveType _type )
		{
			var go = GameObject.CreatePrimitive(_type);
			var mesh = go.GetComponent<MeshFilter>().sharedMesh;
			DestroyObject(go);
			return mesh;
		}

		#endregion

		#region Materials

		/// <summary>The render pipeline the project runs, as far as the test materials care.</summary>
		public enum EPipeline
		{
			BuiltIn,
			Urp,
			Hdrp,
		}

		public static EPipeline Pipeline
		{
			get
			{
				var pipeline = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
				if (pipeline == null)
					return EPipeline.BuiltIn;
				return pipeline.GetType().Name.Contains("HDRender") ? EPipeline.Hdrp : EPipeline.Urp;
			}
		}

		/// <summary>A lit opaque material in whatever pipeline the project runs (Standard, URP/Lit, HDRP/Lit).</summary>
		public static Material CreateOpaque( string _name, Color _color, float _metallic = 0, float _smoothness = 0.4f )
		{
			string shaderName;
			switch (Pipeline)
			{
				case EPipeline.Urp: shaderName = "Universal Render Pipeline/Lit"; break;
				case EPipeline.Hdrp: shaderName = "HDRP/Lit"; break;
				default: shaderName = "Standard"; break;
			}

			var shader = Shader.Find(shaderName);
			if (shader == null)
				throw new System.InvalidOperationException($"shader '{shaderName}' not found");

			var material = new Material(shader) { name = _name };
			SetColor(material, _color);
			SetFloat(material, "_Metallic", _metallic);
			SetFloat(material, "_Glossiness", _smoothness);   // Standard
			SetFloat(material, "_Smoothness", _smoothness);   // URP, HDRP
			return material;
		}

		/// <summary>A flat colour that ignores lights, in whatever pipeline the project runs.</summary>
		public static Material CreateUnlit( Color _color )
		{
			string shaderName;
			switch (Pipeline)
			{
				case EPipeline.Urp: shaderName = "Universal Render Pipeline/Unlit"; break;
				case EPipeline.Hdrp: shaderName = "HDRP/Unlit"; break;
				default: shaderName = "Unlit/Color"; break;
			}

			var material = new Material(Shader.Find(shaderName)) { name = "Unlit" };
			SetColor(material, _color);
			return material;
		}

		private static void SetColor( Material _material, Color _color )
		{
			// Standard calls it _Color, URP and HDRP _BaseColor, unlit HDRP _UnlitColor
			if (_material.HasProperty("_Color"))
				_material.SetColor("_Color", _color);
			if (_material.HasProperty("_BaseColor"))
				_material.SetColor("_BaseColor", _color);
			if (_material.HasProperty("_UnlitColor"))
				_material.SetColor("_UnlitColor", _color);
		}

		private static void SetFloat( Material _material, string _property, float _value )
		{
			if (_material.HasProperty(_property))
				_material.SetFloat(_property, _value);
		}

		/// Alpha blended, the way each pipeline's material inspector sets it (Standard: Fade mode).
		public static Material CreateTransparent( string _name, Color _color )
		{
			var material = CreateOpaque(_name, _color, 0, 0.9f);
			if (Pipeline == EPipeline.Urp)
			{
				material.SetFloat("_Surface", 1);
				material.SetFloat("_Blend", 0);
				material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
				material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
				material.SetFloat("_ZWrite", 0);
				material.SetOverrideTag("RenderType", "Transparent");
				material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
				material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
				return material;
			}

			if (Pipeline == EPipeline.Hdrp)
			{
				material.SetFloat("_SurfaceType", 1);
				material.SetFloat("_EnableBlendModePreserveSpecularLighting", 0);   // specular is added on top of the alpha: colour above alpha
				material.SetFloat("_BlendMode", 0);
				material.SetFloat("_ZWrite", 0);
				material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
				material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
				material.SetFloat("_AlphaSrcBlend", (float)UnityEngine.Rendering.BlendMode.One);
				material.SetFloat("_AlphaDstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
				material.SetOverrideTag("RenderType", "Transparent");
				material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
				material.EnableKeyword("_BLENDMODE_ALPHA");
				material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
#if UNITY_EDITOR
				// HDRP's own inspector code derives the pass states and keywords from the properties
				var utils = System.Type.GetType("UnityEditor.Rendering.HighDefinition.HDShaderUtils, Unity.RenderPipelines.HighDefinition.Editor");
				utils?.GetMethod("ResetMaterialKeywords", new[] { typeof(Material) })?.Invoke(null, new object[] { material });
#endif
				return material;
			}

			material.SetFloat("_Mode", 2);
			material.SetOverrideTag("RenderType", "Transparent");
			material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
			material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
			material.SetInt("_ZWrite", 0);
			material.DisableKeyword("_ALPHATEST_ON");
			material.EnableKeyword("_ALPHABLEND_ON");
			material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
			material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
			return material;
		}

		#endregion

		#region Prefab roots (in memory)

		/// The skinned tentacle: root with Animator, bone chain along +Y, SkinnedMeshRenderer on a child.
		public static GameObject BuildTentacle( Material _material, out Mesh _mesh )
		{
			var root = new GameObject("TestTentacle");
			var bones = new Transform[TentacleBones];
			_mesh = BuildTentacleMesh(TentacleBones, 2f, 0.28f, out _, out var positions);
			for (int i = 0; i < TentacleBones; i++)
			{
				var bone = new GameObject($"Bone{i}").transform;
				bone.SetParent(i == 0 ? root.transform : bones[i - 1], false);
				bone.position = positions[i];
				bones[i] = bone;
			}

			AddSkinnedRenderer(root, bones, _mesh, _material);
			root.AddComponent<Animator>();
			return root;
		}

		/// The skinned puppet: hips, spine, head, two arms (upper + lower), two legs. T pose, 1.8 m tall.
		public static GameObject BuildPuppet( Material _body, out Mesh _mesh )
		{
			var root = new GameObject("TestPuppet");
			var names = new[] { "Hips", "Spine", "Head", "ArmL", "ForearmL", "ArmR", "ForearmR", "LegL", "LegR" };
			var parents = new[] { -1, 0, 1, 1, 3, 1, 5, 0, 0 };
			var positions = new[]
			{
				new Vector3(0, 0.9f, 0), new Vector3(0, 1.15f, 0), new Vector3(0, 1.6f, 0),
				new Vector3(-0.25f, 1.5f, 0), new Vector3(-0.7f, 1.5f, 0),
				new Vector3(0.25f, 1.5f, 0), new Vector3(0.7f, 1.5f, 0),
				new Vector3(-0.12f, 0.85f, 0), new Vector3(0.12f, 0.85f, 0),
			};

			var bones = new Transform[PuppetBones];
			var bind = new Matrix4x4[PuppetBones];
			for (int i = 0; i < PuppetBones; i++)
			{
				var bone = new GameObject(names[i]).transform;
				bone.SetParent(parents[i] < 0 ? root.transform : bones[parents[i]], false);
				bone.position = positions[i];
				bones[i] = bone;
				bind[i] = Matrix4x4.Translate(-positions[i]);
			}

			var capsule = Primitive(PrimitiveType.Capsule);   // 2 m high, radius 0.5: scaled per part
			var sphere = Primitive(PrimitiveType.Sphere);
			var cube = Primitive(PrimitiveType.Cube);
			var parts = new List<Part>
			{
				new() { Mesh = cube, Bone = 0, Matrix = Matrix4x4.TRS(new Vector3(0, 0.95f, 0), Quaternion.identity, new Vector3(0.38f, 0.2f, 0.22f)) },
				new() { Mesh = capsule, Bone = 1, Matrix = Matrix4x4.TRS(new Vector3(0, 1.3f, 0), Quaternion.identity, new Vector3(0.36f, 0.3f, 0.22f)) },
				new() { Mesh = sphere, Bone = 2, Matrix = Matrix4x4.TRS(new Vector3(0, 1.7f, 0), Quaternion.identity, Vector3.one * 0.26f) },
				new() { Mesh = capsule, Bone = 3, Matrix = Matrix4x4.TRS(new Vector3(-0.47f, 1.5f, 0), Quaternion.Euler(0, 0, 90), new Vector3(0.12f, 0.24f, 0.12f)) },
				new() { Mesh = capsule, Bone = 4, Matrix = Matrix4x4.TRS(new Vector3(-0.92f, 1.5f, 0), Quaternion.Euler(0, 0, 90), new Vector3(0.1f, 0.22f, 0.1f)) },
				new() { Mesh = capsule, Bone = 5, Matrix = Matrix4x4.TRS(new Vector3(0.47f, 1.5f, 0), Quaternion.Euler(0, 0, 90), new Vector3(0.12f, 0.24f, 0.12f)) },
				new() { Mesh = capsule, Bone = 6, Matrix = Matrix4x4.TRS(new Vector3(0.92f, 1.5f, 0), Quaternion.Euler(0, 0, 90), new Vector3(0.1f, 0.22f, 0.1f)) },
				new() { Mesh = capsule, Bone = 7, Matrix = Matrix4x4.TRS(new Vector3(-0.12f, 0.45f, 0), Quaternion.identity, new Vector3(0.16f, 0.42f, 0.16f)) },
				new() { Mesh = capsule, Bone = 8, Matrix = Matrix4x4.TRS(new Vector3(0.12f, 0.45f, 0), Quaternion.identity, new Vector3(0.16f, 0.42f, 0.16f)) },
			};

			_mesh = BuildRigidMesh(parts, bind, "TestPuppet");
			AddSkinnedRenderer(root, bones, _mesh, _body);
			root.AddComponent<Animator>();
			return root;
		}

		/// Stem + base opaque (submesh 0), bowl transparent (submesh 1): one renderer, two materials.
		public static GameObject BuildGoblet( Material _stemMaterial, Material _bowlMaterial, out Mesh _mesh )
		{
			var stem = new List<Vector2>
			{
				new(0.0f, 0.0f), new(0.45f, 0.0f), new(0.45f, 0.06f), new(0.12f, 0.12f), new(0.07f, 0.2f),
				new(0.07f, 0.8f), new(0.0f, 0.82f),
			};
			var bowl = new List<Vector2>
			{
				new(0.0f, 0.82f), new(0.1f, 0.83f), new(0.4f, 1.1f), new(0.52f, 1.45f), new(0.5f, 1.8f), new(0.46f, 1.8f),
				new(0.48f, 1.45f), new(0.36f, 1.12f), new(0.0f, 0.9f),
			};

			var stemMesh = BuildLathe(stem, "stem");
			var bowlMesh = BuildLathe(bowl, "bowl");
			var combined = new CombineInstance[2];
			combined[0].mesh = stemMesh;
			combined[1].mesh = bowlMesh;
			combined[0].transform = combined[1].transform = Matrix4x4.identity;
			_mesh = new Mesh { name = "TestGoblet" };
			_mesh.CombineMeshes(combined, false, false);   // two submeshes
			DestroyObject(stemMesh);
			DestroyObject(bowlMesh);

			var root = new GameObject("TestGoblet");
			root.AddComponent<MeshFilter>().sharedMesh = _mesh;
			root.AddComponent<MeshRenderer>().sharedMaterials = new[] { _stemMaterial, _bowlMaterial };
			return root;
		}

		/// Three renderers, three materials: polished metal cup, wooden base, cloth ribbon.
		public static GameObject BuildTrophy( Material _metal, Material _wood, Material _cloth, out Mesh[] _meshes )
		{
			var cup = BuildLathe(new List<Vector2>
			{
				new(0.0f, 0.3f), new(0.12f, 0.3f), new(0.1f, 0.5f), new(0.28f, 0.62f), new(0.5f, 1.2f), new(0.55f, 1.55f),
				new(0.5f, 1.55f), new(0.45f, 1.2f), new(0.24f, 0.7f), new(0.0f, 0.62f),
			}, "TestTrophyCup");
			var baseMesh = Primitive(PrimitiveType.Cube);
			var ribbon = BuildLathe(new List<Vector2> { new(0.37f, 0.6f), new(0.37f, 0.8f) }, "TestTrophyRibbon", 24);
			_meshes = new[] { cup, ribbon };

			var root = new GameObject("TestTrophy");
			AddPart(root, "Cup", cup, _metal, Vector3.zero, Vector3.one);
			AddPart(root, "Base", baseMesh, _wood, new Vector3(0, 0.15f, 0), new Vector3(1.1f, 0.3f, 1.1f));
			AddPart(root, "Ribbon", ribbon, _cloth, Vector3.zero, Vector3.one);
			return root;
		}

		private static void AddPart( GameObject _root, string _name, Mesh _mesh, Material _material, Vector3 _position, Vector3 _scale )
		{
			var go = new GameObject(_name);
			go.transform.SetParent(_root.transform, false);
			go.transform.localPosition = _position;
			go.transform.localScale = _scale;
			go.AddComponent<MeshFilter>().sharedMesh = _mesh;
			go.AddComponent<MeshRenderer>().sharedMaterial = _material;
		}

		private static void AddSkinnedRenderer( GameObject _root, Transform[] _bones, Mesh _mesh, Material _material )
		{
			var go = new GameObject("Mesh");
			go.transform.SetParent(_root.transform, false);
			var skinned = go.AddComponent<SkinnedMeshRenderer>();
			skinned.sharedMesh = _mesh;
			skinned.bones = _bones;
			skinned.rootBone = _bones[0];
			skinned.sharedMaterial = _material;
		}

		private static void DestroyObject( UnityEngine.Object _obj )
		{
			if (Application.isPlaying)
				UnityEngine.Object.Destroy(_obj);
			else
				UnityEngine.Object.DestroyImmediate(_obj);
		}

		#endregion

#if UNITY_EDITOR
		#region Asset generation (editor)

		[MenuItem(StringConstants.MENU_HEADER + "3D Icons/Dev: Rebuild Test Models")]
		private static void MenuRebuild() => RebuildAssets();

		/// Generates the assets unless they exist. Edit mode only: assets written from inside a running PlayMode test
		/// were measured to lose their references (mesh, controller clip) when loaded back from disk.
		public static void EnsureAssets()
		{
			if (Application.isPlaying)
				return;

			if (AssetDatabase.LoadAssetAtPath<GameObject>(TentaclePath) == null
				|| AssetDatabase.LoadAssetAtPath<GameObject>(PuppetPath) == null
				|| AssetDatabase.LoadAssetAtPath<GameObject>(GobletPath) == null
				|| AssetDatabase.LoadAssetAtPath<GameObject>(TrophyPath) == null)
				RebuildAssets();
		}

		public static void RebuildAssets()
		{
			// Starting from an empty folder: rewriting assets in place left the new prefabs without their meshes and controllers
			if (AssetDatabase.IsValidFolder(Folder))
			{
				AssetDatabase.DeleteAsset(Folder);
				AssetDatabase.Refresh();
			}

			EnsureFolder(Folder);

			var tentacleMaterial = SaveAsset(CreateOpaque("TestTentacle", new Color(0.35f, 0.75f, 0.45f), 0, 0.5f), "TestTentacle.mat");
			var skinMaterial = SaveAsset(CreateOpaque("TestPuppetBody", new Color(0.85f, 0.4f, 0.35f)), "TestPuppetBody.mat");
			var stemMaterial = SaveAsset(CreateOpaque("TestGobletStem", new Color(0.8f, 0.7f, 0.3f), 1, 0.8f), "TestGobletStem.mat");
			var bowlMaterial = SaveAsset(CreateTransparent("TestGobletBowl", new Color(0.7f, 0.85f, 1f, 0.3f)), "TestGobletBowl.mat");
			var metal = SaveAsset(CreateOpaque("TestTrophyMetal", new Color(0.95f, 0.8f, 0.3f), 0.5f, 0.85f), "TestTrophyMetal.mat");
			var wood = SaveAsset(CreateOpaque("TestTrophyWood", new Color(0.4f, 0.25f, 0.12f), 0, 0.3f), "TestTrophyWood.mat");
			var cloth = SaveAsset(CreateOpaque("TestTrophyCloth", new Color(0.7f, 0.1f, 0.15f), 0, 0.05f), "TestTrophyCloth.mat");

			// Tentacle
			var tentacle = BuildTentacle(tentacleMaterial, out var tentacleMesh);
			SaveAsset(tentacleMesh, "TestTentacle.mesh.asset");
			var wave = CreateTentacleClip();
			SaveAsset(wave, "TestTentacleWave.anim");
			tentacle.GetComponent<Animator>().runtimeAnimatorController = CreateController("TestTentacle", wave);
			SavePrefab(tentacle, TentaclePath);

			// Puppet
			var puppet = BuildPuppet(skinMaterial, out var puppetMesh);
			SaveAsset(puppetMesh, "TestPuppet.mesh.asset");
			var wavingClip = CreatePuppetClip();
			SaveAsset(wavingClip, "TestPuppetWave.anim");
			puppet.GetComponent<Animator>().runtimeAnimatorController = CreateController("TestPuppet", wavingClip);
			SavePrefab(puppet, PuppetPath);

			// Goblet
			var goblet = BuildGoblet(stemMaterial, bowlMaterial, out var gobletMesh);
			SaveAsset(gobletMesh, "TestGoblet.mesh.asset");
			SavePrefab(goblet, GobletPath);

			// Trophy
			var trophy = BuildTrophy(metal, wood, cloth, out var trophyMeshes);
			SaveAsset(trophyMeshes[0], "TestTrophyCup.mesh.asset");
			SaveAsset(trophyMeshes[1], "TestTrophyRibbon.mesh.asset");
			SavePrefab(trophy, TrophyPath);

			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
			Debug.Log($"ICON3D test models written to {Folder}");
		}

		private static AnimationClip CreateTentacleClip()
		{
			var clip = new AnimationClip { name = "TestTentacleWave", frameRate = 30 };
			for (int i = 1; i < TentacleBones; i++)
			{
				var keys = new Keyframe[5];
				for (int k = 0; k < keys.Length; k++)
					keys[k] = new Keyframe(k * 0.25f, Mathf.Sin((k / 4f * 2 - i * 0.5f) * Mathf.PI) * 28f);
				keys[4].value = keys[0].value;   // loops cleanly
				clip.SetCurve(BonePath(i), typeof(Transform), "localEulerAnglesRaw.z", new AnimationCurve(keys));
			}

			MakeLoop(clip);
			return clip;
		}

		private static AnimationClip CreatePuppetClip()
		{
			var clip = new AnimationClip { name = "TestPuppetWave", frameRate = 30 };
			// Right arm swings up and down, forearm follows, spine sways: three bones, large angles
			clip.SetCurve("Hips/Spine/ArmR", typeof(Transform), "localEulerAnglesRaw.z",
				new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.25f, 70), new Keyframe(0.5f, 0), new Keyframe(0.75f, 70), new Keyframe(1, 0)));
			clip.SetCurve("Hips/Spine/ArmR/ForearmR", typeof(Transform), "localEulerAnglesRaw.z",
				new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.25f, -40), new Keyframe(0.5f, 0), new Keyframe(0.75f, -40), new Keyframe(1, 0)));
			clip.SetCurve("Hips/Spine", typeof(Transform), "localEulerAnglesRaw.z",
				new AnimationCurve(new Keyframe(0, -6), new Keyframe(0.5f, 6), new Keyframe(1, -6)));
			MakeLoop(clip);
			return clip;
		}

		private static string BonePath( int _index )
		{
			var path = "Bone0";
			for (int i = 1; i <= _index; i++)
				path += $"/Bone{i}";
			return path;
		}

		private static void MakeLoop( AnimationClip _clip )
		{
			var settings = AnimationUtility.GetAnimationClipSettings(_clip);
			settings.loopTime = true;
			AnimationUtility.SetAnimationClipSettings(_clip, settings);
		}

		private static AnimatorController CreateController( string _name, AnimationClip _clip )
		{
			var path = $"{Folder}/{_name}.controller";
			AssetDatabase.DeleteAsset(path);
			var controller = AnimatorController.CreateAnimatorControllerAtPath(path);
			// AddMotion creates the state AND keeps it (and the clip reference) as part of the saved controller; a state
			// added by hand is a separate sub asset that is silently not written unless it is marked dirty itself
			controller.AddMotion(_clip);
			EditorUtility.SetDirty(controller);
			AssetDatabase.SaveAssets();
			return controller;
		}

		private static T SaveAsset<T>( T _asset, string _fileName ) where T : UnityEngine.Object
		{
			var path = $"{Folder}/{_fileName}";
			AssetDatabase.DeleteAsset(path);
			AssetDatabase.CreateAsset(_asset, path);
			return _asset;
		}

		private static void SavePrefab( GameObject _root, string _path )
		{
			AssetDatabase.SaveAssets();   // meshes, clips and controllers on disk before a prefab refers to them
			PrefabUtility.SaveAsPrefabAsset(_root, _path);
			UnityEngine.Object.DestroyImmediate(_root);
		}

		private static void EnsureFolder( string _path )
		{
			if (AssetDatabase.IsValidFolder(_path))
				return;
			var parent = System.IO.Path.GetDirectoryName(_path).Replace('\\', '/');
			EnsureFolder(parent);
			AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(_path));
		}

		#endregion
#endif
	}
}
