#version 330 core
out vec4 FragColor;

in VS_OUT {
    vec3 FragPos;
    vec2 TexCoords;
    vec3 TangentLightPos;
    vec3 TangentViewPos;
    vec3 TangentFragPos;
} fs_in;

uniform sampler2D diffuseMap;
uniform sampler2D normalMap;
uniform sampler2D depthMap;

uniform float heightScale;

float depth = 0.0;

vec2 ReliefMapping(vec2 texCoords, vec3 viewDir) {
    // dv points from eye to fragment
    vec2 dv = -heightScale * (1.0/viewDir.z) * viewDir.xy;

    // delta * max_i == 1
    float delta = 0.1;
    int max_i = 20;

    float currentDepth = 0.0;
    float actual_dep = 0.0;
    for (int i=0; i<max_i; i++) {
        currentDepth += delta;
        actual_dep = texture(depthMap, texCoords + currentDepth * dv).x;
        if (actual_dep <= currentDepth) { // currentDepth deeper than actual depth
            break;
        }
    }

    delta *= 0.5;
    currentDepth -= delta;
    for (int i=0; i<32; i++) {
        delta *= 0.5;
        actual_dep = texture(depthMap, texCoords + currentDepth * dv).z;
        if (actual_dep <= currentDepth) { // currentDepth deeper than actual depth
            currentDepth -= delta;
        }
        else {
            currentDepth += delta;
        }
    }

    depth = currentDepth;
    return texCoords + currentDepth * dv;
}

float shadow (vec2 texCoords, vec3 lightDir) {
    // dv points from fragment to light
    vec2 dv = heightScale * (1.0/lightDir.z) * lightDir.xy;

    int check_times = 30;
    for (int i=1; i<check_times; i++) {
        if (depth * (1.0 - (i*1.0/check_times)) > texture(depthMap, texCoords + depth * (i*1.0/check_times) * dv).z) {
            return 0.4;
        }
    }
    return 1.0;
}

void main()
{
    // offset texture coordinates with Parallax Mapping
    vec3 viewDir = normalize(fs_in.TangentViewPos - fs_in.TangentFragPos);
    vec2 texCoords = fs_in.TexCoords;
    
    // texCoords = ParallaxMapping(fs_in.TexCoords, viewDir);
    texCoords = ReliefMapping(fs_in.TexCoords, viewDir);
    // texCoords = fs_in.TexCoords;
    if(texCoords.x > 1.0 || texCoords.y > 1.0 || texCoords.x < 0.0 || texCoords.y < 0.0)
        discard;

    // obtain normal from normal map
    vec3 normal = texture(normalMap, texCoords).rgb;
    normal = normalize(normal * 2.0 - 1.0);   
   
    // get diffuse color
    vec3 color = texture(diffuseMap, texCoords).rgb;
    // ambient
    vec3 ambient = 0.1 * color;
    // diffuse
    vec3 lightDir = normalize(fs_in.TangentLightPos - vec3(texCoords, heightScale*depth));
    float diff = max(dot(lightDir, normal), 0.0) * shadow(texCoords, lightDir);
    vec3 diffuse = 1.5 * diff * color;
    // specular    
    vec3 reflectDir = reflect(-lightDir, normal);
    vec3 halfwayDir = normalize(lightDir + viewDir);  
    float spec = pow(max(dot(normal, halfwayDir), 0.0), 32.0);

    vec3 specular = vec3(0.2) * spec;
    FragColor = vec4(ambient + diffuse + specular, 1.0);
}