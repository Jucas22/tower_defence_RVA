using System.Collections;
using UnityEngine;

[AddComponentMenu("AI/Monster Animation Sequencer")]
public class MonsterAnimationSequencer : MonoBehaviour
{
    [Header("Animator")]
    public Animator animator; // el Animator del monstruo

    [Header("Secuencia")]
    [Tooltip("Nombres de los estados en el Animator (ej: Walk, Jump, Attack, Climb, Die)")]
    public string[] stateNames;
    [Tooltip("Duración en segundos para cada estado. Si <= 0 y 'useClipLengthIfZero' activo, usará la duración del clip")]
    public float[] durations;
    public bool loopSequence = true;
    public bool randomOrder = false;
    public bool useClipLengthIfZero = true;

    [Header("Comportamiento")]
    [Tooltip("Si true, si se reproduce un estado cuyo nombre coincide con 'stopStateName', se parará la secuencia")]
    public string stopStateName = "Die";

    Coroutine sequenceCoroutine;

    void Reset()
    {
        animator = GetComponent<Animator>();
    }

    void Start()
    {
        // Validación básica
        if (animator == null)
        {
            Debug.LogWarning("[MonsterAnimationSequencer] No hay Animator asignado.");
            return;
        }

        if (stateNames == null || stateNames.Length == 0)
        {
            Debug.LogWarning("[MonsterAnimationSequencer] No hay estados configurados en stateNames.");
            return;
        }

        // Asegura que durations tenga la misma longitud que stateNames
        if (durations == null || durations.Length != stateNames.Length)
        {
            durations = new float[stateNames.Length];
        }

        // Inicia la secuencia
        sequenceCoroutine = StartCoroutine(SequenceLoop());
    }

    IEnumerator SequenceLoop()
    {
        var indices = new int[stateNames.Length];
        for (int i = 0; i < indices.Length; i++) indices[i] = i;

        while (true)
        {
            if (randomOrder)
                Shuffle(indices);

            for (int k = 0; k < indices.Length; k++)
            {
                int i = indices[k];
                string state = stateNames[i];

                // Reproducir la animación
                PlayState(state);

                // Si es estado de stop, terminamos la secuencia (opcional)
                if (!string.IsNullOrEmpty(stopStateName) && state == stopStateName)
                {
                    yield break;
                }

                // Calcular duración a esperar
                float wait = durations[i];
                if (wait <= 0f && useClipLengthIfZero)
                {
                    float clipLen;
                    if (TryGetClipLength(state, out clipLen))
                        wait = clipLen;
                }

                // Si no se pudo obtener duración, esperamos 1s por defecto para evitar lock
                if (wait <= 0f) wait = 1f;

                yield return new WaitForSeconds(wait);
            }

            if (!loopSequence)
                yield break;
        }
    }

    // Reproduce un estado por nombre (usa CrossFade para mejor transición)
    public void PlayState(string stateName, float transition = 0.1f)
    {
        if (animator == null || string.IsNullOrEmpty(stateName)) return;
        int hash = Animator.StringToHash(stateName);
        animator.CrossFade(hash, transition);
    }

    // Reproduce una animación puntual y detiene la secuencia automática si hay una en curso
    public void PlayOneShot(string stateName, float durationFallback = 0.1f)
    {
        if (sequenceCoroutine != null)
        {
            StopCoroutine(sequenceCoroutine);
            sequenceCoroutine = null;
        }

        PlayState(stateName);
        // Si quieres que vuelva a la secuencia después, puedes iniciar otra coroutine aquí.
        StartCoroutine(ResumeAfter(stateName, durationFallback));
    }

    IEnumerator ResumeAfter(string stateName, float fallback)
    {
        float wait = fallback;
        if (useClipLengthIfZero)
        {
            float clipLen;
            if (TryGetClipLength(stateName, out clipLen))
                wait = clipLen;
        }

        yield return new WaitForSeconds(wait);

        // Reinicia la secuencia si estaba configurada para loop
        if (sequenceCoroutine == null && loopSequence)
            sequenceCoroutine = StartCoroutine(SequenceLoop());
    }

    // Intenta obtener la longitud de un AnimationClip por nombre desde el controlador
    bool TryGetClipLength(string clipName, out float length)
    {
        length = 0f;
        if (animator == null || animator.runtimeAnimatorController == null) return false;

        var clips = animator.runtimeAnimatorController.animationClips;
        for (int i = 0; i < clips.Length; i++)
        {
            if (clips[i].name == clipName)
            {
                length = clips[i].length;
                return true;
            }
        }
        // A veces los nombres en controller son "Base Layer.StateName" o varían, intenta contains
        for (int i = 0; i < clips.Length; i++)
        {
            if (clips[i].name.Contains(clipName))
            {
                length = clips[i].length;
                return true;
            }
        }
        return false;
    }

    // Mezcla aleatoria de índices
    void Shuffle(int[] array)
    {
        for (int i = 0; i < array.Length; i++)
        {
            int r = Random.Range(i, array.Length);
            int tmp = array[i];
            array[i] = array[r];
            array[r] = tmp;
        }
    }

    // Métodos públicos útiles para control externo
    public void StartSequence()
    {
        if (sequenceCoroutine == null)
            sequenceCoroutine = StartCoroutine(SequenceLoop());
    }

    public void StopSequence()
    {
        if (sequenceCoroutine != null)
        {
            StopCoroutine(sequenceCoroutine);
            sequenceCoroutine = null;
        }
    }
}