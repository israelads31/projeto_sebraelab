using UnityEngine;

namespace RenovaLab
{
    [RequireComponent(typeof(BoxCollider), typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class RepairStation : MonoBehaviour
    {
        [SerializeField, Range(0, 2)] int tool;

        static readonly string[] Diagnoses = { "Rachadura", "Umidade", "Tinta descascando" };
        static readonly string[] Lessons = {
            "Observe se a fissura volta ou cresce.",
            "Trate a origem da umidade antes de pintar.",
            "A tinta adere melhor a uma base limpa e firme."
        };

        Mesh mesh;
        MeshRenderer visual;
        readonly Vector3[] vertices = new Vector3[4];
        readonly Vector2[] uv = new Vector2[4];
        public bool Identified { get; private set; }
        public bool Repaired => Progress >= 1;
        public bool AcceptsApplication => Identified && !Repaired;
        public float Progress { get; private set; }
        public int Tool => tool;
        public string Diagnosis => Diagnoses[tool];
        public string Lesson => Lessons[tool];

        public void Prepare()
        {
            visual = GetComponent<MeshRenderer>();
            mesh = new Mesh { name = "Sobreposição de dano" };
            mesh.MarkDynamic();
            UpdateVisual();
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            mesh.RecalculateNormals();
            GetComponent<MeshFilter>().sharedMesh = mesh;
        }

        public void Identify() => Identified = true;

        public bool Apply(float seconds)
        {
            if (!AcceptsApplication) return false;
            Progress = Mathf.Clamp01(Progress + seconds / 2.1f);
            UpdateVisual();
            return Repaired;
        }

        public void ResetRepair()
        {
            Identified = false;
            Progress = 0;
            UpdateVisual();
        }

        void UpdateVisual()
        {
            visual.enabled = !Repaired;
            float left = Progress - .5f;
            vertices[0] = new Vector3(left, -.5f, 0);
            vertices[1] = new Vector3(.5f, -.5f, 0);
            vertices[2] = new Vector3(.5f, .5f, 0);
            vertices[3] = new Vector3(left, .5f, 0);
            uv[0] = new Vector2(Progress, 0);
            uv[1] = new Vector2(1, 0);
            uv[2] = new Vector2(1, 1);
            uv[3] = new Vector2(Progress, 1);
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.RecalculateBounds();
        }

        void OnDestroy()
        {
            if (mesh) Destroy(mesh);
        }
    }
}
