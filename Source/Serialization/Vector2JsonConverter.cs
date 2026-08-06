using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.QuickTools.Serialization;

internal sealed class Vector2JsonConverter : JsonConverter<Vector2> {
    public override Vector2 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        if (reader.TokenType != JsonTokenType.StartObject) {
            throw new JsonException("Expected JSON object for Vector2.");
        }

        float x = 0f;
        float y = 0f;

        while (reader.Read()) {
            if (reader.TokenType == JsonTokenType.EndObject) {
                return new Vector2(x, y);
            }

            if (reader.TokenType != JsonTokenType.PropertyName) {
                throw new JsonException("Expected property name in Vector2 object.");
            }

            string? name = reader.GetString();
            reader.Read();

            switch (name) {
                case "x":
                case "X":
                    x = reader.GetSingle();
                    break;
                case "y":
                case "Y":
                    y = reader.GetSingle();
                    break;
                default:
                    reader.Skip();
                    break;
            }
        }

        throw new JsonException("Unexpected end of Vector2 object.");
    }

    public override void Write(Utf8JsonWriter writer, Vector2 value, JsonSerializerOptions options) {
        writer.WriteStartObject();
        writer.WriteNumber("x", value.X);
        writer.WriteNumber("y", value.Y);
        writer.WriteEndObject();
    }
}
