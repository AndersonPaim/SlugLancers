using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

public class ScriptableObjectsImporter : EditorWindow
{
    static readonly char[] ListSeparator = { '|' };
    // Listas de enum aceitam '|' e ',' (o Google Sheets exporta chips como "A, B")
    static readonly char[] EnumListSeparator = { '|', ',' };

    static readonly Dictionary<string, string> ColumnAliases =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "name", "displayName" },
        };

    static readonly HashSet<string> BuiltInProps = new HashSet<string>
    {
        "m_Script", "m_Name", "m_ObjectHideFlags", "m_EditorHideFlags",
        "m_EditorClassIdentifier", "m_CorrespondingSourceObject",
        "m_PrefabInstance", "m_PrefabAsset",
    };

    const string ScriptsRoot = "Assets/_Project/Scripts";

    [SerializeField] string selectedTypeName;
    [SerializeField] string csvPath = "";
    [SerializeField] string outputFolder = "Assets/Data";
    [SerializeField] string search = "";

    List<Type> allTypes = new List<Type>();
    Vector2 typeScroll, logScroll;
    readonly List<(int level, string msg)> log = new List<(int, string)>();
    GUIStyle logStyle;

    [MenuItem("Tools/CSV/ScriptableObjects Importer")]
    static void Open()
    {
        var w = GetWindow<ScriptableObjectsImporter>("ScriptableObjects Importer");
        w.minSize = new Vector2(760, 440);
    }

    void OnEnable() => RefreshTypes();

    void RefreshTypes()
    {
        var result = new List<Type>();

        if (AssetDatabase.IsValidFolder(ScriptsRoot))
        {
            foreach (string guid in AssetDatabase.FindAssets("t:MonoScript", new[] { ScriptsRoot }))
            {
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(AssetDatabase.GUIDToAssetPath(guid));
                var t = script != null ? script.GetClass() : null;

                if (t != null && typeof(ScriptableObject).IsAssignableFrom(t) && IsUserType(t) && !result.Contains(t))
                    result.Add(t);
            }
        }

        allTypes = result.OrderBy(t => t.Name).ToList();
    }

    static bool IsUserType(Type t)
    {
        if (t.IsAbstract || t.IsGenericType || t.IsNested) return false;
        if (typeof(EditorWindow).IsAssignableFrom(t) || typeof(Editor).IsAssignableFrom(t)) return false;

        string asm = t.Assembly.GetName().Name;
        string[] blocked = { "Unity", "System", "Mono", "mscorlib", "netstandard", "nunit", "Bee", "PlayerBuildProgramLibrary" };
        foreach (var b in blocked)
            if (asm.StartsWith(b, StringComparison.Ordinal)) return false;

        return true;
    }

    Type SelectedType => allTypes.FirstOrDefault(t => t.AssemblyQualifiedName == selectedTypeName);

    void OnGUI()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            DrawTypeList();
            DrawSettings();
        }
    }

    void DrawTypeList()
    {
        using (new EditorGUILayout.VerticalScope("box", GUILayout.Width(240)))
        {
            GUILayout.Label("ScriptableObject types", EditorStyles.boldLabel);
            search = EditorGUILayout.TextField(search, EditorStyles.toolbarSearchField);
            if (GUILayout.Button("Refresh list")) RefreshTypes();

            if (allTypes.Count == 0)
                EditorGUILayout.HelpBox($"No ScriptableObject found in {ScriptsRoot}.", MessageType.Info);

            typeScroll = EditorGUILayout.BeginScrollView(typeScroll);
            foreach (var t in allTypes)
            {
                if (!string.IsNullOrEmpty(search) &&
                    t.Name.IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0) continue;

                bool selected = t.AssemblyQualifiedName == selectedTypeName;
                var prev = GUI.backgroundColor;
                if (selected) GUI.backgroundColor = new Color(0.4f, 0.7f, 1f);
                if (GUILayout.Button(t.Name, EditorStyles.miniButtonMid)) SelectType(t);
                GUI.backgroundColor = prev;
            }
            EditorGUILayout.EndScrollView();
        }
    }

    void DrawSettings()
    {
        using (new EditorGUILayout.VerticalScope())
        {
            var type = SelectedType;
            if (type == null)
            {
                EditorGUILayout.HelpBox("Select a type from the list on the left.", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField(type.FullName, EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                csvPath = EditorGUILayout.TextField("CSV file", csvPath);
                if (GUILayout.Button("...", GUILayout.Width(30)))
                {
                    string p = EditorUtility.OpenFilePanel("Choose CSV", "", "csv,tsv,txt");
                    if (!string.IsNullOrEmpty(p)) { csvPath = p; GUI.FocusControl(null); }
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                outputFolder = EditorGUILayout.TextField("Output folder", outputFolder);
                if (GUILayout.Button("...", GUILayout.Width(30)))
                {
                    string f = EditorUtility.OpenFolderPanel("Output folder", "Assets", "");
                    if (!string.IsNullOrEmpty(f))
                    {
                        f = f.Replace('\\', '/');
                        string dp = Application.dataPath.Replace('\\', '/');
                        if (f.StartsWith(dp)) { outputFolder = "Assets" + f.Substring(dp.Length); GUI.FocusControl(null); }
                        else Add(2, "The folder must be inside Assets/.");
                    }
                }
            }

            bool canRun = !string.IsNullOrWhiteSpace(csvPath) && outputFolder.Trim().StartsWith("Assets");
            using (new EditorGUI.DisabledScope(!canRun))
            {
                if (GUILayout.Button("Import", GUILayout.Height(28))) Run(type);
            }

            DrawLog();
        }
    }

    void DrawLog()
    {
        if (log.Count == 0) return;
        logStyle ??= new GUIStyle(EditorStyles.label) { richText = true, wordWrap = true };

        EditorGUILayout.LabelField("Result", EditorStyles.boldLabel);
        logScroll = EditorGUILayout.BeginScrollView(logScroll, "box");
        foreach (var (level, msg) in log)
        {
            string color = level == 2 ? "#ff6b6b" : level == 1 ? "#e6b800" : "#8fd18f";
            string icon = level == 2 ? "✖" : level == 1 ? "⚠" : "•";
            GUILayout.Label($"<color={color}>{icon}</color> {msg}", logStyle);
        }
        EditorGUILayout.EndScrollView();
    }

    void Add(int level, string msg) => log.Add((level, msg));

    static string Key(Type t, string k) => $"ScriptableObjectsImporter.{t.FullName}.{k}";

    void SelectType(Type t)
    {
        selectedTypeName = t.AssemblyQualifiedName;
        csvPath = EditorPrefs.GetString(Key(t, "csv"), "");
        outputFolder = EditorPrefs.GetString(Key(t, "folder"), $"Assets/Data/{t.Name}");
        log.Clear();
        GUI.FocusControl(null);
    }

    void SavePrefs(Type t)
    {
        EditorPrefs.SetString(Key(t, "csv"), csvPath);
        EditorPrefs.SetString(Key(t, "folder"), outputFolder);
    }

    class ImportContext
    {
        public string IdField;
        public Dictionary<Type, Dictionary<string, UnityEngine.Object>> Index =
            new Dictionary<Type, Dictionary<string, UnityEngine.Object>>();
    }

    class ColumnMap
    {
        public int Index;
        public string Column;
        public string PropertyPath;
        public Type ClrType;
    }

    sealed class ScratchSo : IDisposable
    {
        public readonly ScriptableObject Obj;
        public readonly SerializedObject So;
        public ScratchSo(Type t) { Obj = CreateInstance(t); So = new SerializedObject(Obj); }
        public void Dispose() { So.Dispose(); DestroyImmediate(Obj); }
    }

    void Run(Type type)
    {
        log.Clear();
        SavePrefs(type);

        try
        {
            RunInternal(type);
        }
        catch (Exception e)
        {
            Add(2, "Unexpected error: " + e.Message);
            Debug.LogException(e);
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    void RunInternal(Type type)
    {
        var ctx = new ImportContext();
        string full = ResolvePath(csvPath);
        if (!File.Exists(full)) { Add(2, "File not found: " + full); return; }

        AssetDatabase.Refresh();

        string text = File.ReadAllText(full, Encoding.UTF8);
        char delim = CsvParser.DetectDelimiter(text);
        var rows = CsvParser.Parse(text, delim);
        if (rows.Count < 2) { Add(2, "The CSV needs a header row and at least one data row."); return; }

        string[] header = rows[0].Select(h => h.Trim()).ToArray();

        const int idCol = 0;
        ctx.IdField = header[0];
        if (ctx.IdField.Length == 0) { Add(2, "The first column (id) needs a header name."); return; }

        var columns = new List<ColumnMap>();
        using (var scratch = new ScratchSo(type))
        {
            if (scratch.So.FindProperty(ctx.IdField) == null)
            {
                Add(2, $"Type {type.Name} has no field '{ctx.IdField}'.");
                return;
            }

            for (int c = 0; c < header.Length; c++)
            {
                string col = header[c];
                if (col.Length == 0) continue;

                var p = FindProperty(scratch.So, col);
                if (p == null)
                {
                    Add(1, $"Column '{col}' ignored: no serialized field with that name in {type.Name}.");
                    continue;
                }

                var fi = FindField(type, p.name);
                columns.Add(new ColumnMap { Index = c, Column = col, PropertyPath = p.propertyPath, ClrType = fi?.FieldType });

                string kind = p.isArray && p.propertyType != SerializedPropertyType.String
                    ? "list" : p.propertyType.ToString();
                Add(0, $"{col} → {p.name} ({kind})");
            }
        }

        var index = GetIdIndex(type, ctx);
        var seen = new HashSet<string>();
        int created = 0, updated = 0, rowsWithErrors = 0;
        string folder = outputFolder.Trim().Replace('\\', '/').TrimEnd('/');

        for (int r = 1; r < rows.Count; r++)
        {
            var row = rows[r];
            string id = Cell(row, idCol).Trim();
            EditorUtility.DisplayProgressBar("Importing CSV", id, (float)r / rows.Count);

            if (id.Length == 0) { Add(2, $"Line {r + 1}: empty id, line skipped."); rowsWithErrors++; continue; }
            if (!seen.Add(id)) Add(1, $"Line {r + 1}: id '{id}' is repeated in the CSV; the last line wins.");

            index.TryGetValue(id, out var asset);
            bool isNew = asset == null;

            if (isNew)
            {
                var instance = CreateInstance(type);
                instance.name = SanitizeFileName(id);
                EnsureFolder(folder);
                string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{instance.name}.asset");
                AssetDatabase.CreateAsset(instance, path);
                asset = instance;
            }

            bool rowOk = true;
            var so = new SerializedObject(asset);
            so.Update();

            foreach (var col in columns)
            {
                string raw = Cell(row, col.Index).Trim();
                var p = so.FindProperty(col.PropertyPath);
                if (p == null) continue;

                // Célula vazia ou "/" (marcador de vazio da planilha) limpa o campo
                if (raw.Length == 0 || raw == "/")
                {
                    ClearProperty(p);
                    continue;
                }

                if (!TrySetValue(p, col.ClrType, raw, ctx, out string err))
                {
                    Add(2, $"Line {r + 1}, column '{col.Column}': {err} (value: '{raw}')");
                    rowOk = false;
                }
            }

            var idp = so.FindProperty(ctx.IdField);
            if (idp != null && idp.propertyType == SerializedPropertyType.String) idp.stringValue = id;

            so.ApplyModifiedPropertiesWithoutUndo();
            so.Dispose();

            if (!rowOk) rowsWithErrors++;

            EditorUtility.SetDirty(asset);
            index[id] = asset;

            if (isNew) created++;
            else
            {
                updated++;
                Add(0, $"Updated existing asset: {AssetDatabase.GetAssetPath(asset)}");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string summary = $"Import finished: {created} new, {updated} updated, {rowsWithErrors} row(s) with errors. " +
                         $"Detected delimiter: '{(delim == '\t' ? "TAB" : delim.ToString())}'.";
        Add(rowsWithErrors > 0 ? 1 : 0, summary);
        Debug.Log("[ScriptableObjects Importer] " + summary);
    }

    static string Cell(string[] row, int i) => i < row.Length ? row[i] : "";

    static string ResolvePath(string p)
    {
        p = p.Trim();
        if (Path.IsPathRooted(p)) return p;
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        return Path.GetFullPath(Path.Combine(projectRoot, p));
    }

    static string SanitizeFileName(string s)
    {
        foreach (char c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
        return s;
    }

    static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder)) return;
        string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
        if (string.IsNullOrEmpty(parent)) return;
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
    }

    static SerializedProperty FindProperty(SerializedObject so, string column)
    {
        var exact = so.FindProperty(column);
        if (exact != null && !BuiltInProps.Contains(exact.name)) return exact;

        ColumnAliases.TryGetValue(column, out string alias);

        var it = so.GetIterator();
        if (it.Next(true))
        {
            do
            {
                if (BuiltInProps.Contains(it.name)) continue;
                string n = it.name.TrimStart('_');
                if (n.StartsWith("m_", StringComparison.Ordinal)) n = n.Substring(2);

                if (string.Equals(n, column, StringComparison.OrdinalIgnoreCase) ||
                    (alias != null && string.Equals(n, alias, StringComparison.OrdinalIgnoreCase)))
                    return so.FindProperty(it.name);
            } while (it.Next(false));
        }
        return null;
    }

    static FieldInfo FindField(Type t, string name)
    {
        const BindingFlags f = BindingFlags.Instance | BindingFlags.Public |
                               BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        for (var c = t; c != null && c != typeof(ScriptableObject) && c != typeof(UnityEngine.Object); c = c.BaseType)
        {
            var fi = c.GetField(name, f) ?? c.GetField($"<{name}>k__BackingField", f);
            if (fi != null) return fi;
        }
        return null;
    }

    static Type ElementType(Type t)
    {
        if (t == null) return null;
        if (t.IsArray) return t.GetElementType();
        if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(List<>)) return t.GetGenericArguments()[0];
        return null;
    }

    static bool TrySetValue(SerializedProperty p, Type clrType, string raw, ImportContext ctx, out string err)
    {
        err = null;

        if (p.isArray && p.propertyType != SerializedPropertyType.String)
        {
            var elemType = ElementType(clrType);
            bool enumList = elemType != null && elemType.IsEnum;

            var parts = raw.Split(enumList ? EnumListSeparator : ListSeparator, StringSplitOptions.RemoveEmptyEntries)
                           .Select(s => s.Trim()).Where(s => s.Length > 0).ToArray();

            p.arraySize = parts.Length;
            for (int i = 0; i < parts.Length; i++)
            {
                if (!TrySetValue(p.GetArrayElementAtIndex(i), elemType, parts[i], ctx, out err))
                {
                    err = $"list item {i}: {err}";
                    return false;
                }
            }
            return true;
        }

        switch (p.propertyType)
        {
            case SerializedPropertyType.String:
                p.stringValue = raw;
                return true;

            case SerializedPropertyType.Integer:
            {
                if (!TryNumber(raw, out double d)) { err = "expected an integer"; return false; }
                if (d != Math.Floor(d)) { err = "expected an integer, got a decimal"; return false; }
                if (clrType == typeof(long) || clrType == typeof(ulong)) { p.longValue = (long)d; return true; }
                if (d < int.MinValue || d > int.MaxValue) { err = "out of int range"; return false; }
                p.intValue = (int)d;
                return true;
            }

            case SerializedPropertyType.Float:
            {
                if (!TryNumber(raw, out double d)) { err = "expected a number"; return false; }
                if (clrType == typeof(double)) p.doubleValue = d; else p.floatValue = (float)d;
                return true;
            }

            case SerializedPropertyType.Boolean:
            {
                string s = raw.ToLowerInvariant();
                if (s == "true" || s == "1" || s == "yes" || s == "sim" || s == "x" || s == "verdadeiro") { p.boolValue = true; return true; }
                if (s == "false" || s == "0" || s == "no" || s == "nao" || s == "não" || s == "falso") { p.boolValue = false; return true; }
                err = "expected true/false (or 1/0, yes/no)";
                return false;
            }

            case SerializedPropertyType.Enum:
                return TrySetEnum(p, clrType, raw, out err);

            case SerializedPropertyType.ObjectReference:
            {
                var obj = ResolveReference(clrType ?? typeof(UnityEngine.Object), raw, ctx);
                if (obj == null) { err = $"reference not found (type {clrType?.Name ?? "?"})"; return false; }
                p.objectReferenceValue = obj;
                return true;
            }

            case SerializedPropertyType.Vector2:
            {
                if (!TryFloats(raw, 2, out var v)) { err = "expected 'x y'"; return false; }
                p.vector2Value = new Vector2(v[0], v[1]);
                return true;
            }

            case SerializedPropertyType.Vector3:
            {
                if (!TryFloats(raw, 3, out var v)) { err = "expected 'x y z'"; return false; }
                p.vector3Value = new Vector3(v[0], v[1], v[2]);
                return true;
            }

            case SerializedPropertyType.Color:
            {
                if (!ColorUtility.TryParseHtmlString(raw, out var col)) { err = "expected a color in #RRGGBB format"; return false; }
                p.colorValue = col;
                return true;
            }

            default:
                err = $"serialized type '{p.propertyType}' is not supported by the importer";
                return false;
        }
    }

    static bool TryNumber(string s, out double d)
    {
        s = s.Trim();
        if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out d)) return true;
        return double.TryParse(s.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out d);
    }

    static bool TryFloats(string raw, int count, out float[] values)
    {
        var parts = raw.Split(new[] { ' ', ':' }, StringSplitOptions.RemoveEmptyEntries);
        values = new float[count];
        if (parts.Length != count) return false;
        for (int i = 0; i < count; i++)
        {
            if (!TryNumber(parts[i], out double d)) return false;
            values[i] = (float)d;
        }
        return true;
    }

    static bool TrySetEnum(SerializedProperty p, Type clrType, string raw, out string err)
    {
        err = null;
        bool flags = clrType != null && clrType.IsDefined(typeof(FlagsAttribute), false);

        if (flags)
        {
            int mask = 0;
            foreach (string tok in raw.Split(new[] { '|', ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (!Enum.TryParse(clrType, tok.Trim(), true, out object o)) { err = $"invalid enum value '{tok.Trim()}'"; return false; }
                mask |= Convert.ToInt32(o);
            }
            p.enumValueFlag = mask;
            return true;
        }

        string[] names = p.enumNames;
        int idx = Array.FindIndex(names, n => string.Equals(n, raw, StringComparison.OrdinalIgnoreCase));

        if (idx < 0 && clrType != null && Enum.TryParse(clrType, raw, true, out object parsed) && Enum.IsDefined(clrType, parsed))
        {
            string name = parsed.ToString();
            idx = Array.FindIndex(names, n => n == name);
        }

        if (idx < 0) { err = "invalid enum value. Options: " + string.Join(", ", names); return false; }
        p.enumValueIndex = idx;
        return true;
    }

    static void ClearProperty(SerializedProperty p)
    {
        if (p.isArray && p.propertyType != SerializedPropertyType.String) { p.arraySize = 0; return; }
        switch (p.propertyType)
        {
            case SerializedPropertyType.String: p.stringValue = ""; break;
            case SerializedPropertyType.ObjectReference: p.objectReferenceValue = null; break;
            case SerializedPropertyType.Integer: p.intValue = 0; break;
            case SerializedPropertyType.Float: p.floatValue = 0; break;
            case SerializedPropertyType.Boolean: p.boolValue = false; break;
            case SerializedPropertyType.Enum: p.enumValueIndex = 0; break;
            case SerializedPropertyType.Vector2: p.vector2Value = Vector2.zero; break;
            case SerializedPropertyType.Vector3: p.vector3Value = Vector3.zero; break;
            case SerializedPropertyType.Color: p.colorValue = Color.clear; break;
        }
    }

    static UnityEngine.Object ResolveReference(Type t, string raw, ImportContext ctx)
    {
        if (raw.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
        {
            var byPath = AssetDatabase.LoadAssetAtPath(raw, t);
            if (byPath != null) return byPath;
        }

        if (typeof(ScriptableObject).IsAssignableFrom(t))
            return GetIdIndex(t, ctx).TryGetValue(raw, out var a) ? a : null;

        foreach (string guid in AssetDatabase.FindAssets($"{raw} t:{t.Name}"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(path))
                if (o != null && t.IsInstanceOfType(o) && o.name == raw) return o;
        }
        return null;
    }

    static Dictionary<string, UnityEngine.Object> GetIdIndex(Type t, ImportContext ctx)
    {
        if (ctx.Index.TryGetValue(t, out var cached)) return cached;

        var dict = new Dictionary<string, UnityEngine.Object>(StringComparer.Ordinal);
        var assets = new List<ScriptableObject>();

        foreach (string guid in AssetDatabase.FindAssets("t:" + t.Name))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!File.Exists(path)) continue;
            var a = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
            if (a != null && t.IsInstanceOfType(a)) assets.Add(a);
        }

        foreach (var a in assets)
        {
            using (var so = new SerializedObject(a))
            {
                var idp = so.FindProperty(ctx.IdField);
                if (idp != null && idp.propertyType == SerializedPropertyType.String && idp.stringValue.Length > 0)
                    dict.TryAdd(idp.stringValue, a);
            }
        }
        foreach (var a in assets) dict.TryAdd(a.name, a);

        ctx.Index[t] = dict;
        return dict;
    }
}

static class CsvParser
{
    public static char DetectDelimiter(string text)
    {
        int comma = 0, semi = 0, tab = 0;
        bool inQuotes = false;
        foreach (char c in text)
        {
            if (c == '"') inQuotes = !inQuotes;
            else if (!inQuotes)
            {
                if (c == '\n' || c == '\r') break;
                if (c == ',') comma++;
                else if (c == ';') semi++;
                else if (c == '\t') tab++;
            }
        }
        if (semi > comma && semi >= tab) return ';';
        if (tab > comma && tab > semi) return '\t';
        return ',';
    }

    public static List<string[]> Parse(string text, char delim)
    {
        var rows = new List<string[]>();
        var row = new List<string>();
        var sb = new StringBuilder();
        bool inQuotes = false;

        void EndRow()
        {
            row.Add(sb.ToString());
            sb.Clear();
            if (row.Any(s => s.Trim().Length > 0)) rows.Add(row.ToArray());
            row.Clear();
        }

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < text.Length && text[i + 1] == '"') { sb.Append('"'); i++; }
                    else inQuotes = false;
                }
                else sb.Append(c);
            }
            else if (c == '"') inQuotes = true;
            else if (c == delim) { row.Add(sb.ToString()); sb.Clear(); }
            else if (c == '\r' || c == '\n')
            {
                if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n') i++;
                EndRow();
            }
            else sb.Append(c);
        }

        if (sb.Length > 0 || row.Count > 0) EndRow();
        return rows;
    }
}