# GameCraft
# A toolkit for creating games in a voxel-based environment.
# Copyright (C) 2020, Maximilian Bauer (contact@lengo.cc)
# 
# This program is free software: you can redistribute it and/or modify
# it under the terms of the GNU General Public License as published by
# the Free Software Foundation, either version 3 of the License, or
# (at your option) any later version.
# 
# This program is distributed in the hope that it will be useful,
# but WITHOUT ANY WARRANTY; without even the implied warranty of
# MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
# GNU General Public License for more details.
# 
# You should have received a copy of the GNU General Public License
# along with this program. If not, see <http://www.gnu.org/licenses/>.
#

# KNOWN ISSUES
# - If a vertex has muliple UVs, the vertex UVs of the output are incorrect.
#   Workaround: Go into Edit mode, then Mesh > Split > "Faces by Edges".

import types
import bpy
import bmesh

# Following constants can be changed to adapt the script behaviour
ROUND_VECTOR_TO_DIGITS = 4
ROUND_TEXCOORDS_TO_DIGITS = 5

# Global function definitions
def show_message(message = "", title = "Message Box", icon = 'INFO'):
    def draw(self, context):
        self.layout.label(text=message)
    bpy.context.window_manager.popup_menu(draw, title = title, icon = icon)

# Ensures that there's exactly one selected (mesh) model right now and it
# doesn't have any unapplied transformations on it.
if len(bpy.context.selected_objects) == 0:
    show_message("Please select an object which should be exported.",
    "Empty selection", "ERROR")
elif len(bpy.context.selected_objects) > 1:
    show_message("Please only select one object which should be exported.",
    "Too many items selected", "ERROR")
elif not isinstance(bpy.context.selected_objects[0].data, bpy.types.Mesh):
    show_message("Please select an object with a mesh - \
    only these are supported for export.", "Invalid selection", "ERROR")
elif (bpy.context.selected_objects[0].scale.x != 1 or
        bpy.context.selected_objects[0].scale.y != 1 or
        bpy.context.selected_objects[0].scale.z != 1 or
        bpy.context.selected_objects[0].location.x != 0 or
        bpy.context.selected_objects[0].location.y != 0 or
        bpy.context.selected_objects[0].location.z != 0 or
        bpy.context.selected_objects[0].rotation_euler.x != 0 or
        bpy.context.selected_objects[0].rotation_euler.y != 0 or
        bpy.context.selected_objects[0].rotation_euler.z != 0):
        show_message("The object has a custom transformation, "+
        "that needs to be applied before the model can be exported.",
        "Custom transforms detected", "ERROR")
else:    
    # Copy the current mesh data into a new bmesh and triangulate it.
    export_mesh = bmesh.new()
    export_mesh.from_mesh(bpy.context.selected_objects[0].data)
    bmesh.ops.triangulate(export_mesh, faces=export_mesh.faces)
    
    # Make sure the lookup table is ready for vertex and face lookups
    export_mesh.faces.ensure_lookup_table()
    export_mesh.verts.ensure_lookup_table()

    # Fetch the vertex color and UV layers for later.
    try :
        color_layer = export_mesh.loops.layers.color.active
    except:
        color_layer = None
        
    try :
        uv_layer = export_mesh.loops.layers.uv.active
    except:
        uv_layer = None

    # Create vertex and face arrays with the expected sizes.
    vertices = [None]*export_mesh.verts.__len__()
    faces = [None]*export_mesh.faces.__len__()
    
    # Loop through all faces and store them into the face array.
    # At the same time, combine the referenced vertices with their color and 
    # UV data and put them into the vertices array.
    for face in export_mesh.faces:
        faces[face.index] = [face.verts[0].index, face.verts[1].index, face.verts[2].index]
        for loop in face.loops:
            vertex = []
            vertex.append(round(loop.vert.co.x, ROUND_VECTOR_TO_DIGITS))
            vertex.append(round(loop.vert.co.z, ROUND_VECTOR_TO_DIGITS))
            vertex.append(round(loop.vert.co.y, ROUND_VECTOR_TO_DIGITS))
            vertex.append(round(loop.vert.normal.x, ROUND_VECTOR_TO_DIGITS))
            vertex.append(round(loop.vert.normal.z, ROUND_VECTOR_TO_DIGITS))
            vertex.append(round(loop.vert.normal.y, ROUND_VECTOR_TO_DIGITS))
            
            if uv_layer is None:
                vertex.append(0)
                vertex.append(0)
            else:
                uv = loop[uv_layer].uv.to_2d()
                vertex.append(round(uv.x,ROUND_TEXCOORDS_TO_DIGITS))
                vertex.append(round(uv.y,ROUND_TEXCOORDS_TO_DIGITS))

            if color_layer is None:
                vertex.append(0)
                vertex.append(0)
                vertex.append(0)
                vertex.append(255)
            else:
                color = loop[color_layer]
                vertex.append(round(color.x*255))
                vertex.append(round(color.y*255))
                vertex.append(round(color.z*255))
                vertex.append(255)
                
            vertices[loop.vert.index] = vertex

    # Iterate through the created vertex array and create a valid string/XML 
    # representation of each.
    blockVertices = "<vertices format=\"x y z nx ny nz tx ty p1 p2 p3 p4\">\n"
    for v in vertices:
        blockVertices += "\t%s %s %s %s %s %s %s %s %d %d %d %d\n" % (
        v[0], v[1], v[2], v[3], v[4], v[5], v[6], v[7], v[8], v[9], v[10], 
            v[11])
    blockVertices += "</vertices>"    
    
    # Iterate through the created face array and create a valid string/XML 
    # representation of each.
    blockFaces = "<faces>\n"
    for f in faces:
        blockFaces += "\t%s %s %s\n" % (f[0], f[2], f[1])
    blockFaces += "</faces>"    

    # Combine the previously created string representations of vertices and 
    # faces and put them into the clipboard.
    # Confirm the successful end of the operation to the user.
    bpy.context.window_manager.clipboard = blockVertices + "\n" + blockFaces
    show_message("The vertex and face definitions were copied to clipboard!",
    "Operation successful")