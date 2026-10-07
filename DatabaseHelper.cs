using System;
using System.Collections.Generic;
using System.Data.SQLite;
using OpenCvSharp;

namespace Matric_scope
{
    /// <summary>
    /// Measurement/transform behaviour used by a trained custom shape.
    /// IMPORTANT: existing numeric values are preserved for database compatibility.
    /// </summary>
    public enum ShapeTransformMode
    {
        /// <summary>
        /// Width/length axes are forced through the detected centroid.
        /// Best for symmetric shapes and measurements that must pass through center.
        /// </summary>
        Centroid = 0,

        /// <summary>
        /// Keeps the operator's lines offset from the centroid. The engine now uses
        /// a stable whole-silhouette major axis (with narrow-end direction locking)
        /// instead of a single longest polygon edge. Numeric value 1 is preserved so
        /// old database rows remain compatible.
        /// </summary>
        TaperedLongestEdge = 1,

        /// <summary>
        /// Keeps an offset axis at the same RELATIVE location inside the shape.
        /// Example: a width line trained 65% from tip to base stays at about 65% on
        /// a fatter/thinner live stone. The line is then optionally snapped to the
        /// real live contour.
        /// </summary>
        RelativeLandmark = 2
    }

    public class ShapeData
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string ImagePath { get; set; }
        public string TemplateMaskPath { get; set; }

        // Normalized local coordinates produced by CustomShapeEngine.ProjectToLocal().
        public Point2f WidthPt1 { get; set; }
        public Point2f WidthPt2 { get; set; }
        public Point2f LengthPt1 { get; set; }
        public Point2f LengthPt2 { get; set; }

        public float RefAngle { get; set; }

        // New records contain a versioned one-shot contour fingerprint (CSF4|...).
        // SQLite TEXT has no 255-character VARCHAR limitation, so this is suitable
        // for the resampled normalized contour data used by the engine.
        public string ContourData { get; set; }

        public bool SnapToEdge { get; set; }
        public ShapeTransformMode TransformMode { get; set; } = ShapeTransformMode.Centroid;
    }

    public static class DatabaseHelper
    {
        private static readonly string dbPath = "History.db";
        private static readonly string connectionString = $"Data Source={dbPath};Version=3;";

        public static void InitializeDatabase()
        {
            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();

                string sql = @"
                CREATE TABLE IF NOT EXISTS CustomShapes (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL,
                    ImagePath TEXT,
                    W1X REAL, W1Y REAL, W2X REAL, W2Y REAL,
                    L1X REAL, L1Y REAL, L2X REAL, L2Y REAL,
                    RefAngle REAL,
                    ContourData TEXT,
                    TemplateMaskPath TEXT,
                    SnapToEdge INTEGER DEFAULT 0,
                    TransformMode INTEGER DEFAULT 0
                );";

                using (var cmd = new SQLiteCommand(sql, conn))
                    cmd.ExecuteNonQuery();

                // Safe upgrades for older History.db files.
                TryAddColumn(conn, "ALTER TABLE CustomShapes ADD COLUMN TemplateMaskPath TEXT;");
                TryAddColumn(conn, "ALTER TABLE CustomShapes ADD COLUMN ContourData TEXT;");
                TryAddColumn(conn, "ALTER TABLE CustomShapes ADD COLUMN RefAngle REAL DEFAULT 0;");
                TryAddColumn(conn, "ALTER TABLE CustomShapes ADD COLUMN SnapToEdge INTEGER DEFAULT 0;");
                TryAddColumn(conn, "ALTER TABLE CustomShapes ADD COLUMN TransformMode INTEGER DEFAULT 0;");
            }
        }

        private static void TryAddColumn(SQLiteConnection conn, string sql)
        {
            try
            {
                using (var cmd = new SQLiteCommand(sql, conn))
                    cmd.ExecuteNonQuery();
            }
            catch
            {
                // Column already exists. Intentionally ignored.
            }
        }

        public static void SaveShape(ShapeData shape)
        {
            if (shape == null)
                throw new ArgumentNullException(nameof(shape));

            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();

                const string sql = @"
                INSERT INTO CustomShapes
                (Name, ImagePath,
                 W1X, W1Y, W2X, W2Y,
                 L1X, L1Y, L2X, L2Y,
                 RefAngle, ContourData, TemplateMaskPath,
                 SnapToEdge, TransformMode)
                VALUES
                (@Name, @ImagePath,
                 @W1X, @W1Y, @W2X, @W2Y,
                 @L1X, @L1Y, @L2X, @L2Y,
                 @RefAngle, @ContourData, @TemplateMaskPath,
                 @SnapToEdge, @TransformMode);";

                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Name", shape.Name ?? "Unnamed");
                    cmd.Parameters.AddWithValue("@ImagePath", shape.ImagePath ?? "");

                    cmd.Parameters.AddWithValue("@W1X", shape.WidthPt1.X);
                    cmd.Parameters.AddWithValue("@W1Y", shape.WidthPt1.Y);
                    cmd.Parameters.AddWithValue("@W2X", shape.WidthPt2.X);
                    cmd.Parameters.AddWithValue("@W2Y", shape.WidthPt2.Y);

                    cmd.Parameters.AddWithValue("@L1X", shape.LengthPt1.X);
                    cmd.Parameters.AddWithValue("@L1Y", shape.LengthPt1.Y);
                    cmd.Parameters.AddWithValue("@L2X", shape.LengthPt2.X);
                    cmd.Parameters.AddWithValue("@L2Y", shape.LengthPt2.Y);

                    cmd.Parameters.AddWithValue("@RefAngle", shape.RefAngle);
                    cmd.Parameters.AddWithValue("@ContourData", shape.ContourData ?? "");
                    cmd.Parameters.AddWithValue("@TemplateMaskPath", shape.TemplateMaskPath ?? "");
                    cmd.Parameters.AddWithValue("@SnapToEdge", shape.SnapToEdge ? 1 : 0);
                    cmd.Parameters.AddWithValue("@TransformMode", (int)shape.TransformMode);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static List<ShapeData> GetAllShapes()
        {
            var list = new List<ShapeData>();

            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();

                const string sql = "SELECT * FROM CustomShapes ORDER BY Id;";
                using (var cmd = new SQLiteCommand(sql, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int rawMode = reader["TransformMode"] != DBNull.Value
                            ? Convert.ToInt32(reader["TransformMode"])
                            : 0;

                        ShapeTransformMode mode = Enum.IsDefined(typeof(ShapeTransformMode), rawMode)
                            ? (ShapeTransformMode)rawMode
                            : ShapeTransformMode.Centroid;

                        list.Add(new ShapeData
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            Name = reader["Name"]?.ToString() ?? "",
                            ImagePath = reader["ImagePath"] != DBNull.Value ? reader["ImagePath"].ToString() : "",

                            WidthPt1 = new Point2f(ToSingle(reader["W1X"]), ToSingle(reader["W1Y"])),
                            WidthPt2 = new Point2f(ToSingle(reader["W2X"]), ToSingle(reader["W2Y"])),
                            LengthPt1 = new Point2f(ToSingle(reader["L1X"]), ToSingle(reader["L1Y"])),
                            LengthPt2 = new Point2f(ToSingle(reader["L2X"]), ToSingle(reader["L2Y"])),

                            RefAngle = reader["RefAngle"] != DBNull.Value ? Convert.ToSingle(reader["RefAngle"]) : 0f,
                            ContourData = reader["ContourData"] != DBNull.Value ? reader["ContourData"].ToString() : "",
                            TemplateMaskPath = reader["TemplateMaskPath"] != DBNull.Value ? reader["TemplateMaskPath"].ToString() : "",
                            SnapToEdge = reader["SnapToEdge"] != DBNull.Value && Convert.ToInt32(reader["SnapToEdge"]) == 1,
                            TransformMode = mode
                        });
                    }
                }
            }

            return list;
        }

        private static float ToSingle(object value)
        {
            return value == null || value == DBNull.Value ? 0f : Convert.ToSingle(value);
        }

        public static void DeleteShape(int shapeId)
        {
            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                const string sql = "DELETE FROM CustomShapes WHERE Id = @Id;";

                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", shapeId);
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
