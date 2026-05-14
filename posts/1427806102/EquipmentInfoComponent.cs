using System.Collections.Generic;
using MagicaCloth2;
using UnityEngine;

namespace SgrEngine
{
    public abstract class EquipmentInfoComponent : MonoBehaviour
    {
        public CharacterEquipComponent owner;
        public List<SkinnedMeshRenderer> skinnedMeshRenderers;
        public List<MagicaCloth> magicaClothList;
        public List<ColliderComponent> colliderComponentList;
        public EquipmentMeshType equipmentType;

        public string equipmentGUID = "";
        public bool isInit = false;

        private RendererMaterialManager rendererMaterialManager;

        protected void EquipMesh(bool isCachedInstance)
        {
            List<SkinnedMeshRenderer> renderers = skinnedMeshRenderers;
            Dictionary<string, Transform> boneMap = owner.boneMap;

            if (isCachedInstance)
            {
                for (int i = 0; i < renderers.Count; ++i)
                {
                    renderers[i].gameObject.SetActive(true);
                }
            }
            else
            {
                // swap skinned mesh renderers bones
                for (int i = 0; i < renderers.Count; ++i)
                {
                    Transform[] bones = renderers[i].bones;
                    Transform[] newBones = new Transform[bones.Length];
                    for (int j = 0; j < bones.Length; ++j)
                    {
                        Transform bone = bones[j];
                        if (!boneMap.TryGetValue(bone.name, out newBones[j]))
                        {
                            // Is the bone the renderer itself?
                            if (bone.name == renderers[i].name)
                            {
                                newBones[j] = renderers[i].transform;
                                boneMap[bone.name] = newBones[j];
                            }
                            else
                            {
                                // Rebuild Bone
                                Stack<Transform> boneHierarchy = new Stack<Transform>();
                                boneHierarchy.Push(bone);
                                Transform foundBone = null;
                                while (bone.parent != null)
                                {
                                    bone = bone.parent;
                                    boneMap.TryGetValue(bone.name, out foundBone);
                                    if (foundBone != null)
                                    {
                                        break;
                                    }
                                    boneHierarchy.Push(bone);
                                }

                                if (foundBone != null)
                                {
                                    while (boneHierarchy.Count > 0)
                                    {
                                        Transform insertBone = boneHierarchy.Pop();

                                        insertBone.SetParent(foundBone, false);
                                        foundBone = insertBone;
                                        boneMap[foundBone.name] = foundBone;

                                        if (boneHierarchy.Count == 0)
                                        {
                                            newBones[j] = foundBone;
                                        }
                                    }
                                }
                                else
                                {
                                    Debug.LogError($"骨骼绑定失败: 找不到 {bone.name} 的对应骨骼。请检查装备与角色的骨骼结构是否兼容");
                                    return;
                                }
                            }
                        }
                    }
                    renderers[i].bones = newBones;
                    // root bone
                    Transform rootBone = renderers[i].rootBone;
                    if (rootBone != null && boneMap.TryGetValue(rootBone.name, out Transform newBone))
                    {
                        renderers[i].rootBone = newBone;
                    }
                }
            }
        }

        protected void EquipMagicaCloth(bool isCachedInstance)
        {
            Dictionary<string, Transform> boneMap = owner.boneMap;

            if (isCachedInstance)
            {
                for (int i = 0; i < magicaClothList.Count; ++i)
                {
                    magicaClothList[i].gameObject.SetActive(true);
                }
                for (int i = 0; i < colliderComponentList.Count; ++i)
                {
                    colliderComponentList[i].gameObject.SetActive(true);
                    AddColliderComponent(colliderComponentList[i]);
                }
            }
            else
            {
                for (int i = 0; i < magicaClothList.Count; ++i)
                {
                    magicaClothList[i].Initialize();
                    magicaClothList[i].DisableAutoBuild();
                }

                for (int i = 0; i < magicaClothList.Count; ++i)
                {
                    magicaClothList[i].ReplaceTransform(boneMap);
                    // 将 Player 身上的 ColliderComponent 加入模型 MagicaCloth 的 ColliderList 中
                    if (magicaClothList[i].gameObject.name != "Magica Cloth Collisionless")
                    {
                        List<ColliderComponent> colliderList = magicaClothList[i].SerializeData.colliderCollisionConstraint.colliderList;
                        foreach (ColliderComponent colliderComponent in owner.colliderMap.Values)
                        {
                            colliderList.Add(colliderComponent);
                        }
                    }
                }

                // 将模型自己独有的 ColliderComponent 设置到 Player 身上
                for (int i = 0; i < colliderComponentList.Count; ++i)
                {
                    Transform parent = colliderComponentList[i].transform.parent;
                    if (parent && boneMap.ContainsKey(parent.name))
                    {
                        Transform newParent = boneMap[parent.name];

                        // After changing the parent, you need to write back the local posture and align it.
                        colliderComponentList[i].transform.GetLocalPositionAndRotation(out Vector3 localPosition, out Quaternion localRotation);
                        colliderComponentList[i].transform.SetParent(newParent);
                        colliderComponentList[i].transform.SetLocalPositionAndRotation(localPosition, localRotation);
                        AddColliderComponent(colliderComponentList[i]);
                    }
                }

                for (int i = 0; i < magicaClothList.Count; ++i)
                {
                    // I disabled the automatic build, so I build it manually.
                    magicaClothList[i].BuildAndRun();
                }
            }
        }

        protected void UnequipMesh(bool isDestroy)
        {
            List<SkinnedMeshRenderer> renderers = skinnedMeshRenderers;

            if (isDestroy)
            {
                for (int i = 0; i < renderers.Count; ++i)
                {
                    Destroy(renderers[i].gameObject);
                }
            }
            else
            {
                for (int i = 0; i < renderers.Count; ++i)
                {
                    renderers[i].gameObject.SetActive(false);
                }
            }
        }

        protected void UnequipMagicaCloth(bool isDestroy)
        {
            if (isDestroy)
            {
                for (int i = 0; i < magicaClothList.Count; ++i)
                {
                    Destroy(magicaClothList[i].gameObject);
                }
                for (int i = 0; i < colliderComponentList.Count; ++i)
                {
                    RemoveColliderComponent(colliderComponentList[i]);
                    Destroy(colliderComponentList[i].gameObject);
                }
            }
            else
            {
                for (int i = 0; i < magicaClothList.Count; ++i)
                {
                    magicaClothList[i].gameObject.SetActive(false);
                }
                for (int i = 0; i < colliderComponentList.Count; ++i)
                {
                    RemoveColliderComponent(colliderComponentList[i]);
                    colliderComponentList[i].gameObject.SetActive(false);
                }
            }
        }

        protected void UpdateCharacterRenderer()
        {
            if (rendererMaterialManager != null || owner.TryGetComponent(out rendererMaterialManager))
            {
                rendererMaterialManager.UpdateRenderers();
            }
        }

        private void AddColliderComponent(ColliderComponent colliderComponent)
        {
            owner.colliderMap.Add(colliderComponent.name, colliderComponent);
            AddColliderComponent(owner.bodyInfoComponent.magicaClothList, colliderComponent);
            AddColliderComponent(owner.bootInfoComponent.magicaClothList, colliderComponent);
            AddColliderComponent(owner.gloveInfoComponent.magicaClothList, colliderComponent);
            AddColliderComponent(owner.headInfoComponent.magicaClothList, colliderComponent);
            AddColliderComponent(owner.hairInfoComponent.magicaClothList, colliderComponent);
        }

        private void AddColliderComponent(List<MagicaCloth> magicaClothList, ColliderComponent colliderComponent)
        {
            for (int i = 0; i < magicaClothList.Count; ++i)
            {
                magicaClothList[i].SerializeData.colliderCollisionConstraint.colliderList.Add(colliderComponent);
            }
        }

        private void RemoveColliderComponent(ColliderComponent colliderComponent)
        {
            owner.colliderMap.Remove(colliderComponent.name);
            RemoveColliderComponent(owner.bodyInfoComponent.magicaClothList, colliderComponent);
            RemoveColliderComponent(owner.bootInfoComponent.magicaClothList, colliderComponent);
            RemoveColliderComponent(owner.gloveInfoComponent.magicaClothList, colliderComponent);
            RemoveColliderComponent(owner.headInfoComponent.magicaClothList, colliderComponent);
            RemoveColliderComponent(owner.hairInfoComponent.magicaClothList, colliderComponent);
        }

        private void RemoveColliderComponent(List<MagicaCloth> magicaClothList, ColliderComponent colliderComponent)
        {
            for (int i = 0; i < magicaClothList.Count; ++i)
            {
                magicaClothList[i].SerializeData.colliderCollisionConstraint.colliderList.Remove(colliderComponent);
            }
        }
        
        public virtual bool IsValid()
        {
            return isInit && equipmentGUID != "" && equipmentType != EquipmentMeshType.None;
        }

        public abstract void Equip(bool isCachedInstance);
        public abstract void Unequip(bool isDestroy);
    }
}