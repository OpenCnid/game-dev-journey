using System;
using System.Collections.Generic;
using GameDevHarness.FantasyTrialB;
using UnityEngine;

namespace GameDevHarness.CrucibleReborn
{
    public sealed class ChaseArmsArt : IDisposable
    {
        public Transform Root { get; private set; }
        readonly List<Mesh> meshes = new List<Mesh>();

        public static ChaseArmsArt Create(Transform camera, ArtPalette palette)
        {
            var rig = new ChaseArmsArt();
            rig.Root = RebornWeaponGeometry.Group(camera, "I fitted engineer hands", Vector3.zero);
            Material glove = palette.Make(new Color(.37f,.25f,.16f),0,.24f);
            Material cloth = palette.Make(new Color(.22f,.25f,.24f),0,.12f);
            Material seam = palette.Make(new Color(.53f,.44f,.31f),0,.2f);
            Material buckle = palette.Make(new Color(.36f,.34f,.28f),.7f,.32f);
            ChaseMaterials.Weather(glove,4); ChaseMaterials.Weather(cloth,4);
            ChaseMaterials.Weather(seam,4); ChaseMaterials.Weather(buckle,1);
            var hands = new RebornWeaponGeometry();
            var sleeves = new RebornWeaponGeometry();
            var stitching = new RebornWeaponGeometry();
            var fittings = new RebornWeaponGeometry();
            // Shared with the I primary's grip/support positions, not with H's
            // gauntlet placements. These hands do not accompany satellite copies.
            Matrix4x4 triggerPose = Matrix4x4.TRS(new Vector3(.276f,-.302f,.405f),
                Quaternion.Euler(86,-8,4),Vector3.one);
            Matrix4x4 supportPose = Matrix4x4.TRS(new Vector3(.245f,-.302f,.685f),
                Quaternion.Euler(101,7,-9),Vector3.one);
            Hand(hands,stitching,triggerPose,false);
            Hand(hands,stitching,supportPose,true);
            Sleeve(sleeves,stitching,fittings,new[] {
                new Vector3(.53f,-.54f,.10f),new Vector3(.44f,-.43f,.20f),
                new Vector3(.36f,-.36f,.28f),triggerPose.MultiplyPoint3x4(new Vector3(0,-.065f,0)) });
            Sleeve(sleeves,stitching,fittings,new[] {
                new Vector3(-.36f,-.55f,.17f),new Vector3(-.12f,-.43f,.36f),
                new Vector3(.12f,-.35f,.53f),supportPose.MultiplyPoint3x4(new Vector3(0,-.065f,0)) });
            rig.Bake(hands,glove,"I metacarpals opposed thumbs and twenty phalanges");
            rig.Bake(sleeves,cloth,"I tailored folded forearm cloth");
            rig.Bake(stitching,seam,"I inset seams and wrist bindings");
            rig.Bake(fittings,buckle,"I cuff strap fasteners");
            return rig;
        }

        static void Hand(RebornWeaponGeometry skin, RebornWeaponGeometry seams, Matrix4x4 pose, bool support)
        {
            // Flattened carpal section widens into four metacarpals; the radial
            // heel has its own thenar mass. Rings are anatomical stations.
            skin.Sections(pose,new[] {
                new Vector3(0,-.070f,0),new Vector3(0,-.052f,0),
                new Vector3(.003f,-.028f,0),new Vector3(.001f,-.008f,0),
                new Vector3(0,.017f,0),new Vector3(-.003f,.032f,.004f)
            },new[] { new Vector2(.020f,.016f),new Vector2(.022f,.018f),
                new Vector2(.031f,.022f),new Vector2(.037f,.022f),
                new Vector2(.035f,.019f),new Vector2(.030f,.017f) },24);
            float[] lateral={ .025f,.007f,-.012f,-.029f };
            float[] length={ .069f,.081f,.074f,.058f };
            float[] radius={ .009f,.010f,.0095f,.008f };
            for(int digit=0;digit<4;digit++)
            {
                float x=lateral[digit], l=length[digit], r=radius[digit];
                float spread = support ? 1.08f : 1;
                // Index sits on the trigger; remaining three close under grip.
                float curl = !support && digit==0 ? .032f : .055f;
                var joints=new[] {
                    new Vector3(x,.018f,.003f),
                    new Vector3(x*spread,.018f+l*.28f,.001f),
                    new Vector3(x*spread,.018f+l*.56f,curl*.22f),
                    new Vector3(x*.96f,.018f+l*.64f,curl*.66f),
                    new Vector3(x*.85f,.018f+l*.39f,curl),
                    new Vector3(x*.83f,.018f+l*.21f,curl*.97f),
                    new Vector3(x*.82f,.018f+l*.18f,curl*.91f)
                };
                skin.Sections(pose,joints,new[] {
                    new Vector2(r,r*.86f),new Vector2(r*1.04f,r*.9f),
                    new Vector2(r*.87f,r*.80f),new Vector2(r*.94f,r*.82f),
                    new Vector2(r*.76f,r*.69f),new Vector2(r*.56f,r*.48f),
                    new Vector2(r*.14f,r*.13f) },18);
                // Fine joint seams are narrow shells, not armored knuckle blocks.
                seams.Sections(pose,new[] { joints[2]+new Vector3(-r*.77f,0,-r*.48f),
                    joints[2]+new Vector3(0,.0007f,-r*.80f),
                    joints[2]+new Vector3(r*.77f,0,-r*.48f) },
                    new[] { Vector2.one*.0007f,Vector2.one*.0008f,Vector2.one*.0007f },6);
            }
            skin.Sections(pose,new[] {
                new Vector3(.021f,-.025f,.009f),new Vector3(.039f,-.011f,.012f),
                new Vector3(.052f,.010f,.024f),new Vector3(.046f,.028f,.043f),
                new Vector3(.030f,.035f,.052f),new Vector3(.021f,.035f,.049f)
            },new[] { new Vector2(.017f,.015f),new Vector2(.015f,.013f),
                new Vector2(.012f,.011f),new Vector2(.011f,.010f),
                new Vector2(.008f,.007f),new Vector2(.002f,.002f) },20);
            for(int i=0;i<3;i++)
                seams.Sections(pose,new[] {
                    new Vector3(-.022f,-.06f+i*.006f,-.011f),
                    new Vector3(0,-.06f+i*.006f,-.018f),
                    new Vector3(.022f,-.06f+i*.006f,-.011f)
                },new[] {Vector2.one*.0012f,Vector2.one*.0012f,Vector2.one*.0012f},8);
        }

        static void Sleeve(RebornWeaponGeometry cloth, RebornWeaponGeometry seams,
            RebornWeaponGeometry fittings, Vector3[] landmarks)
        {
            var centers=new List<Vector3>();
            var sizes=new List<Vector2>();
            for(int section=0;section<landmarks.Length-1;section++)
                for(int sample=0;sample<6;sample++)
                {
                    float t=sample/6f;
                    centers.Add(Vector3.Lerp(landmarks[section],landmarks[section+1],t));
                    float along=(section+t)/(landmarks.Length-1);
                    float fold=sample%3==1?.005f:sample%3==2?-.003f:0;
                    sizes.Add(new Vector2(Mathf.Lerp(.075f,.025f,along)+fold,
                        Mathf.Lerp(.061f,.023f,along)+fold*.65f));
                }
            centers.Add(landmarks[landmarks.Length-1]); sizes.Add(new Vector2(.024f,.022f));
            cloth.Sections(Matrix4x4.identity,centers.ToArray(),sizes.ToArray(),24);
            // Long tailored seam follows the outer underside of each folded sleeve.
            var seamCenters=new Vector3[centers.Count];
            var seamRadii=new Vector2[centers.Count];
            for(int i=0;i<centers.Count;i++)
            {
                seamCenters[i]=centers[i]+Vector3.down*(sizes[i].y*.8f);
                seamRadii[i]=Vector2.one*.0011f;
            }
            seams.Sections(Matrix4x4.identity,seamCenters,seamRadii,6);
            Vector3 wrist=landmarks[landmarks.Length-1];
            fittings.Profile(Matrix4x4.TRS(wrist+new Vector3(.025f,0,-.013f),
                Quaternion.identity,Vector3.one),.006f,
                -.014f,-.013f,.014f,-.013f,.019f,0,.014f,.013f,-.014f,.013f,-.018f,0);
        }

        void Bake(RebornWeaponGeometry geometry,Material material,string name)
        {
            Mesh mesh=geometry.Build(name); meshes.Add(mesh);
            RebornWeaponGeometry.Render(Root,mesh,material);
        }

        public void Animate(float elapsed,float speed)
        {
            if(Root==null)return;
            float amount=Mathf.Clamp01(speed);
            Root.localPosition=new Vector3(Mathf.Sin(elapsed*7)*.003f*amount,
                Mathf.Cos(elapsed*14)*.002f*amount,0);
            Root.localRotation=Quaternion.Euler(0,0,Mathf.Sin(elapsed*7)*.25f*amount);
        }

        public void Dispose()
        {
            if(Root==null)return;
            Root.gameObject.SetActive(false);UnityEngine.Object.Destroy(Root.gameObject);Root=null;
            foreach(Mesh mesh in meshes)UnityEngine.Object.Destroy(mesh);
            meshes.Clear();
        }
    }
}
