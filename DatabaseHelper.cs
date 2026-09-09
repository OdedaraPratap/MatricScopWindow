using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using OpenCvSharp;

namespace Matric_scope
{
    public class ShapeData
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string ImagePath { get; set; }
        public string TemplateMaskPath { get; set; } // Stores the 200x200 footprint image

        // These will now store Normalized Coordinates (0.0 to 1.0) for perfect scaling
        public Point2f WidthPt1 { get; set; }
        public Point2f WidthPt2 { get; set; }
        public Point2f LengthPt1 { get; set; }
        public Point2f LengthPt2 { get; set; }

        public float RefAngle { get; set; }
        public string ContourData { get; set; }
        public bool SnapToEdge { get; set; }
        public int TransformVersion { get; set; } = 1;
    }
    public static class DatabaseHelper
    {
        private static string dbPath = "History.db";
        private static string connectionString = $"Data Source={dbPath};Version=3;";

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
                    TransformVersion INTEGER DEFAULT 1
                );";
                using (var cmd = new SQLiteCommand(sql, conn)) { cmd.ExecuteNonQuery(); }

                try { using (var cmd = new SQLiteCommand("ALTER TABLE CustomShapes ADD COLUMN TemplateMaskPath TEXT;", conn)) { cmd.ExecuteNonQuery(); } } catch { }
                try { using (var cmd = new SQLiteCommand("ALTER TABLE CustomShapes ADD COLUMN ContourData TEXT;", conn)) { cmd.ExecuteNonQuery(); } } catch { }
                try { using (var cmd = new SQLiteCommand("ALTER TABLE CustomShapes ADD COLUMN RefAngle REAL DEFAULT 0;", conn)) { cmd.ExecuteNonQuery(); } } catch { }
                try { using (var cmd = new SQLiteCommand("ALTER TABLE CustomShapes ADD COLUMN TransformVersion INTEGER DEFAULT 1;", conn)) { cmd.ExecuteNonQuery(); } } catch { }
            }
        }
        public static void SaveShape(ShapeData shape)
        {
            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                string sql = @"
                INSERT INTO CustomShapes (Name, ImagePath, W1X, W1Y, W2X, W2Y, L1X, L1Y, L2X, L2Y, RefAngle, ContourData, TemplateMaskPath, SnapToEdge, TransformVersion)
                VALUES (@Name, @ImagePath, @W1X, @W1Y, @W2X, @W2Y, @L1X, @L1Y, @L2X, @L2Y, @RefAngle, @ContourData, @TemplateMaskPath, @SnapToEdge, @TransformVersion);";

                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Name", shape.Name);
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
                    cmd.Parameters.AddWithValue("@TransformVersion", shape.TransformVersion);
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
                string sql = "SELECT * FROM CustomShapes;";
                using (var cmd = new SQLiteCommand(sql, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new ShapeData
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            Name = reader["Name"].ToString(),
                            ImagePath = reader["ImagePath"].ToString(),
                            WidthPt1 = new Point2f(Convert.ToSingle(reader["W1X"]), Convert.ToSingle(reader["W1Y"])),
                            WidthPt2 = new Point2f(Convert.ToSingle(reader["W2X"]), Convert.ToSingle(reader["W2Y"])),
                            LengthPt1 = new Point2f(Convert.ToSingle(reader["L1X"]), Convert.ToSingle(reader["L1Y"])),
                            LengthPt2 = new Point2f(Convert.ToSingle(reader["L2X"]), Convert.ToSingle(reader["L2Y"])),
                            RefAngle = reader["RefAngle"] != DBNull.Value ? Convert.ToSingle(reader["RefAngle"]) : 0f,
                            ContourData = reader["ContourData"] != DBNull.Value ? reader["ContourData"].ToString() : "",
                            TemplateMaskPath = reader["TemplateMaskPath"] != DBNull.Value ? reader["TemplateMaskPath"].ToString() : "",
                            SnapToEdge = Convert.ToInt32(reader["SnapToEdge"]) == 1,
                            TransformVersion = reader["TransformVersion"] != DBNull.Value ? Convert.ToInt32(reader["TransformVersion"]) : 1
                        });
                    }
                }
            }
            return list;
        }
        public static void DeleteShape(int shapeId)
        {
            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                string sql = "DELETE FROM CustomShapes WHERE Id = @Id;";
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", shapeId);
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
