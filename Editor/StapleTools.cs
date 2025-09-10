using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Staple
{
    public static class StapleTools
    {
        public const bool AddRigidbodyToColliders = true;

        public static string GetAssetPath(Object o)
        {
            if(o == null)
            {
                return "";
            }

            return AssetDatabase.GetAssetPath(o);
        }

        public static string GetStapleAssetPath(Object o)
        {
            var path = GetAssetPath(o);

            if(string.IsNullOrEmpty(path))
            {
                return path;
            }

            if(IsMainAsset(o) == false)
            {
                var directory = Path.GetDirectoryName(path);
                var fileName = Path.GetFileName(path);

                if (o is Material)
                {
                    fileName = o.name + ".material";
                }

                path = Path.Combine(directory, fileName).Replace('\\', '/');

                if (o is Mesh)
                {
                    path += $":{o.name}";
                }

                return path;
            }

            if(o is Material && path.EndsWith(".mat"))
            {
                return $"{path}erial";
            }

            return path;
        }

        public static bool IsMainAsset(Object o) => AssetDatabase.IsMainAsset(o);

        public static string HexValue(this Color c)
        {
            return $"#{ColorUtility.ToHtmlStringRGBA(c)}";
        }

        public static string HexValue(this Color32 c)
        {
            return $"#{ColorUtility.ToHtmlStringRGBA(c)}";
        }

        private static MaterialMetadata ExportMaterial(Material material)
        {
            if(material == null)
            {
                return null;
            }

            var outValue = new MaterialMetadata();

            var parameters = MaterialEditor.GetMaterialProperties(new Object[] { material });

            foreach (var parameter in parameters)
            {
                var parameterName = parameter.name switch
                {
                    "_Color" => "diffuseColor",
                    "_MainTex" => "diffuseTexture",
                    _ => parameter.name,
                };

                if(parameter.name == "_Cull")
                {
                    var cullFlag = (CullMode)(parameter.propertyType == ShaderPropertyType.Float ?
                        (int)parameter.floatValue : parameter.intValue);

                    outValue.cullingMode = cullFlag switch
                    {
                        CullMode.Off => CullingMode.None,
                        CullMode.Front => CullingMode.Front,
                        CullMode.Back => CullingMode.Back,
                        _ => CullingMode.None,
                    };

                    continue;
                }

                var outParameter = new MaterialParameter();

                switch(parameter.propertyType)
                {
                    case ShaderPropertyType.Color:

                        outParameter.type = MaterialParameterType.Color;
                        outParameter.colorValue = parameter.colorValue;

                        break;

                    case ShaderPropertyType.Float:
                    case ShaderPropertyType.Range:

                        outParameter.type = MaterialParameterType.Float;
                        outParameter.floatValue = parameter.floatValue;

                        break;

                    case ShaderPropertyType.Int:

                        outParameter.type = MaterialParameterType.Int;
                        outParameter.intValue = parameter.intValue;

                        break;

                    case ShaderPropertyType.Texture:

                        outParameter.type = MaterialParameterType.Texture;
                        outParameter.textureValue = GetAssetPath(parameter.textureValue);

                        break;

                    case ShaderPropertyType.Vector:

                        outParameter.type = MaterialParameterType.Vector4;
                        outParameter.vec4Value = new(parameter.vectorValue);

                        break;

                    default:

                        continue;
                }

                outValue.parameters.Add(parameterName, outParameter);
            }

            return outValue;
        }

        [MenuItem("Staple Engine/Tools/Export/Scene")]
        public static void ExportScene()
        {
            var path = EditorUtility.SaveFilePanel("Save Staple Scene", "", "Scene.scene", "scene");

            if(string.IsNullOrEmpty(path))
            {
                return;
            }

            var rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();

            var outValue = new List<SceneObject>();
    
            var counter = 1;

            void Iterate(GameObject gameObject, int parentID)
            {
                var objectID = counter++;

                var outObject = new SceneObject()
                {
                    enabled = gameObject.activeSelf,
                    hierarchyVisibility = EntityHierarchyVisibility.None,
                    ID = objectID,
                    layer = LayerMask.LayerToName(gameObject.layer),
                    name = gameObject.name,
                    parent = parentID,
                    transform = new()
                    {
                        position = new(gameObject.transform.localPosition),
                        rotation = new(gameObject.transform.localRotation.eulerAngles),
                        scale = new(gameObject.transform.localScale),
                    },
                };

                outValue.Add(outObject);

                var components = gameObject.GetComponents<Component>();

                SceneComponent MakeComponent(string typeName)
                {
                    return new()
                    {
                        type = typeName,
                    };
                }

                void AddStaticRigidBody()
                {
                    if(gameObject.GetComponent<Rigidbody>() != null ||
                        outObject.components.Any(x => x.type == "Staple.RigidBody3D"))
                    {
                        return;
                    }

                    var outComponent = MakeComponent("Staple.RigidBody3D");

                    outComponent.data.Add("motionType", "Static");
                    outComponent.data.Add("mass", 1);
                    outComponent.data.Add("freezeRotationX", false);
                    outComponent.data.Add("freezeRotationY", false);
                    outComponent.data.Add("freezeRotationZ", false);
                    outComponent.data.Add("gravityFactor", 0);

                    outObject.components.Add(outComponent);
                }

                foreach (var component in components)
                {
                    switch(component)
                    {
                        case Component c when c is Camera camera:

                            {
                                var outComponent = MakeComponent("Staple.Camera");

                                var clearMode = camera.clearFlags switch
                                {
                                    CameraClearFlags.Depth => CameraClearMode.Depth,
                                    CameraClearFlags.Nothing => CameraClearMode.None,
                                    _ => CameraClearMode.SolidColor,
                                };

                                outComponent.data.Add("clearMode", clearMode.ToString());
                                outComponent.data.Add("cameraType", camera.orthographic ? "Orthographic" : "Perspective");
                                outComponent.data.Add("orthographicSize", camera.orthographicSize);
                                outComponent.data.Add("fov", camera.fieldOfView);
                                outComponent.data.Add("nearPlane", camera.nearClipPlane);
                                outComponent.data.Add("farPlane", camera.farClipPlane);
                                outComponent.data.Add("viewport", new Vector4Holder(camera.rect));
                                outComponent.data.Add("depth", camera.depth);
                                outComponent.data.Add("clearColor", camera.backgroundColor.HexValue());
                                outComponent.data.Add("cullingLayers", camera.cullingMask);

                                outObject.components.Add(outComponent);
                            }

                            break;

                        case Component c when c is AudioListener listener:

                            {
                                var outComponent = MakeComponent("Staple.AudioListener");

                                outObject.components.Add(outComponent);
                            }

                            break;

                        case Component c when c is AudioSource source:

                            {
                                var outComponent = MakeComponent("Staple.AudioSource");

                                outComponent.data.Add("audioClip", GetAssetPath(source.clip));

                                outComponent.data.Add("volume", source.volume);
                                outComponent.data.Add("pitch", source.pitch);
                                outComponent.data.Add("loop", source.loop);
                                outComponent.data.Add("spatial", source.spatialize);
                                outComponent.data.Add("autoplay", source.playOnAwake);

                                outObject.components.Add(outComponent);
                            }

                            break;

                        case Component c when c is MeshRenderer renderer:

                            {
                                var outComponent = MakeComponent("Staple.MeshRenderer");

                                outComponent.data.Add("enabled", renderer.enabled);
                                outComponent.data.Add("forceRenderingOff", renderer.forceRenderingOff);
                                outComponent.data.Add("receiveShadows", renderer.receiveShadows);
                                outComponent.data.Add("sortingLayer", renderer.sortingLayerID);
                                outComponent.data.Add("sortingOrder", renderer.sortingOrder);

                                var filter = gameObject.GetComponent<MeshFilter>();

                                if (filter != null)
                                {
                                    outComponent.data.Add("mesh", GetStapleAssetPath(filter.sharedMesh));
                                }

                                if((renderer.sharedMaterials?.Length ?? 0) > 0)
                                {
                                    var materialPaths = new List<string>();

                                    foreach(var material in renderer.sharedMaterials)
                                    {
                                        materialPaths.Add(GetStapleAssetPath(material));
                                    }

                                    outComponent.data.Add("materials", materialPaths);
                                }

                                outObject.components.Add(outComponent);
                            }

                            break;

                        case Component c when c is SkinnedMeshRenderer renderer:

                            {
                                var outComponent = MakeComponent("Staple.SkinnedMeshRenderer");

                                outComponent.data.Add("enabled", renderer.enabled);
                                outComponent.data.Add("forceRenderingOff", renderer.forceRenderingOff);
                                outComponent.data.Add("receiveShadows", renderer.receiveShadows);
                                outComponent.data.Add("sortingLayer", renderer.sortingLayerID);
                                outComponent.data.Add("sortingOrder", renderer.sortingOrder);

                                outComponent.data.Add("mesh", GetStapleAssetPath(renderer.sharedMesh));

                                if ((renderer.materials?.Length ?? 0) > 0)
                                {
                                    var materialPaths = new List<string>();

                                    foreach (var material in renderer.sharedMaterials)
                                    {
                                        materialPaths.Add(GetStapleAssetPath(material));
                                    }

                                    outComponent.data.Add("materials", materialPaths);
                                }

                                outObject.components.Add(outComponent);
                            }

                            break;

                        case Component c when c is Light light:

                            {
                                var outComponent = MakeComponent("Staple.Light");

                                outComponent.data.Add("type", light.type switch
                                {
                                    LightType.Point => "Point",
                                    LightType.Directional => "Directional",
                                    LightType.Spot => "Spot",
                                    _ => "Point",
                                });

                                outComponent.data.Add("color", light.color.HexValue());

                                outObject.components.Add(outComponent);
                            }

                            break;

                        case Component c when c is BoxCollider collider:

                            {
                                var outComponent = MakeComponent("Staple.BoxCollider3D");

                                outComponent.data.Add("size", new Vector3Holder(collider.size));
                                outComponent.data.Add("position", new Vector3Holder(collider.center));
                                outComponent.data.Add("rotation", new Vector4Holder(Quaternion.identity));

                                outObject.components.Add(outComponent);

                                AddStaticRigidBody();
                            }

                            break;

                        case Component c when c is MeshCollider collider:

                            {
                                var outComponent = MakeComponent("Staple.MeshCollider3D");

                                outComponent.data.Add("mesh", GetStapleAssetPath(collider.sharedMesh));

                                outObject.components.Add(outComponent);

                                AddStaticRigidBody();
                            }

                            break;

                        case Component c when c is SphereCollider collider:

                            {
                                var outComponent = MakeComponent("Staple.SphereCollider3D");

                                outComponent.data.Add("radius", collider.radius);

                                outObject.components.Add(outComponent);

                                AddStaticRigidBody();
                            }

                            break;

                        case Component c when c is CapsuleCollider collider:

                            {
                                var outComponent = MakeComponent("Staple.CapsuleCollider3D");

                                outComponent.data.Add("radius", collider.radius);
                                outComponent.data.Add("height", collider.height);

                                outObject.components.Add(outComponent);

                                AddStaticRigidBody();
                            }

                            break;

                        case Component c when c is Rigidbody rigidBody:

                            {
                                var outComponent = MakeComponent("Staple.RigidBody3D");

                                outComponent.data.Add("motionType", rigidBody.isKinematic ? "Kinematic" :
                                    gameObject.isStatic ? "Static" : "Dynamic");
                                outComponent.data.Add("mass", rigidBody.mass);
                                outComponent.data.Add("freezeRotationX",
                                    rigidBody.constraints.HasFlag(RigidbodyConstraints.FreezeRotationX));
                                outComponent.data.Add("freezeRotationY",
                                    rigidBody.constraints.HasFlag(RigidbodyConstraints.FreezeRotationY));
                                outComponent.data.Add("freezeRotationZ",
                                    rigidBody.constraints.HasFlag(RigidbodyConstraints.FreezeRotationZ));

                                outComponent.data.Add("gravityFactor", rigidBody.useGravity ? 1 : 0);

                                outObject.components.Add(outComponent);
                            }

                            break;

                        case Component c when c is Animator:

                            if(gameObject.GetComponentInChildren<SkinnedMeshRenderer>() != null)
                            {
                                var outComponent = MakeComponent("Staple.SkinnedMeshAnimator");

                                outObject.components.Add(outComponent);

                                outComponent = MakeComponent("Staple.SkinnedMeshInstance");

                                outObject.components.Add(outComponent);

                                outComponent = MakeComponent("Staple.CullingVolume");

                                outObject.components.Add(outComponent);
                            }

                            break;
                    }
                }

                foreach(Transform child in gameObject.transform)
                {
                    Iterate(child.gameObject, objectID);
                }
            }

            foreach (var gameObject in rootObjects)
            {
                Iterate(gameObject, 0);
            }

            var text = JsonConvert.SerializeObject(outValue, Formatting.Indented, new JsonSerializerSettings()
            {
                Converters =
                {
                    new StringEnumConverter(),
                }
            });

            System.IO.File.WriteAllText(path, text);

            EditorUtility.DisplayDialog("Exported", "Scene exported successfully!", "OK");
        }

        [MenuItem("Staple Engine/Tools/Export/Material")]
        public static void ExportMaterial()
        {
            if(Selection.activeObject is not Material material)
            {
                EditorUtility.DisplayDialog("Invalid asset", "You need to select a material", "OK");

                return;
            }

            var path = EditorUtility.SaveFilePanel("Save Staple Material", "", "Material.material", "material");

            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            var outValue = ExportMaterial(material);

            if(outValue == null)
            {
                return;
            }

            var text = JsonConvert.SerializeObject(outValue, Formatting.Indented, new JsonSerializerSettings()
            {
                Converters =
                {
                    new StringEnumConverter(),
                }
            });

            System.IO.File.WriteAllText(path, text);

            EditorUtility.DisplayDialog("Exported", "Material exported successfully!", "OK");
        }
    }
}
