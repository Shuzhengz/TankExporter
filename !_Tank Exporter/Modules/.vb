﻿Imports System.Windows

Imports System.Text
Imports System.IO

Imports System.Math

Imports Aspose.ThreeD
Imports Aspose.ThreeD.Entities
Imports Aspose.ThreeD.Utilities
Imports Aspose.ThreeD.Shading
Module mod_glTF
    Public t_GLTF(0) As _grps
    Public GLTF_Texture_path As String
    Public GLTFgrp(0) As _grps
    Public ctz As New cttools.norm_utilities
    Public GLTF_LOADED As Boolean = False
    Public m_groups() As mgrp_
    Public GLTF_uv2s(100000) As uv_
    Public uv2_total_count As Integer = 0
    Public Sub remove_loaded_glTF()
        If GLTF_LOADED Then
            GLTF_LOADED = False
            For ii = 1 To GLTFgrp.Length - 2
                Gl.glDeleteTextures(1, GLTFgrp(ii).color_Id)
                Gl.glFinish()
                Gl.glDeleteTextures(1, GLTFgrp(ii).color_Id)
                Gl.glFinish()
                Gl.glDeleteLists(GLTFgrp(ii).call_list, 1)
                Gl.glDeleteLists(GLTFgrp(ii).vertex_pick_list, 1)
                Gl.glFinish()
            Next
            frmMain.m_show_GLTF.Visible = False
            frmMain.m_show_GLTF.Checked = False
            frmMain.m_show_GLTF.Enabled = True
            frmMain.m_write_non_tank_primitive.Enabled = False

            ReDim GLTFgrp(0)
            GC.Collect() 'clean up garbage
            GC.WaitForFullGCComplete()
        End If
    End Sub

    Private Sub displayMatrix(m As Matrix4, ByVal name As String)

        Console.WriteLine(name + "------------------------------------------")
        For i = 0 To 3
            For j = 0 To 3
                Console.Write(Round(m.Get(j + i * 3), 6).ToString + vbTab + vbTab)
            Next
            Console.Write(vbCrLf)
        Next

        Console.Write(vbCrLf)

    End Sub
    Public Sub purge_GLTF()

    End Sub


    Public Sub import_glTF()
        'GLTF import sub
        Dim j As UInt32 = 0
        Dim i As UInt32 = 0
        Dim start_index As Integer = 0
        Dim start_vertex As Integer = 0

        frmMain.OpenFileDialog1.InitialDirectory = my.settings.aspose_path

        frmMain.OpenFileDialog1.Filter = "AutoDesk (*.GLTF)|*.GLTF"
        frmMain.OpenFileDialog1.Title = "Import GLTF..."
        If frmMain.OpenFileDialog1.FileName = "OpenFileDialog1" Then
            frmMain.OpenFileDialog1.FileName = ""
        End If
        If Not frmMain.OpenFileDialog1.ShowDialog = Forms.DialogResult.OK Then
            Return
        End If
        my.settings.aspose_path = frmMain.OpenFileDialog1.FileName
        frmComponentView.clear_GLTF_list()
        frmReverseVertexWinding.clear_group_list()

        My.Settings.GLTF_path = Path.GetDirectoryName(frmMain.OpenFileDialog1.FileName)
        frmMain.clean_house()
        remove_loaded_glTF()

        frmMain.info_Label.Text = frmMain.OpenFileDialog1.FileName
        Application.DoEvents()
        Application.DoEvents()
        Application.DoEvents()
        frmMain.pb1.Visible = True
        Application.DoEvents()

        Dim pManager As GLTFSdkManager
        Dim scene As GLTFScene
        pManager = GLTFSdkManager.Create
        scene = GLTFScene.Create(pManager, "My Scene")
        Dim fileformat As Integer = Skill.GLTFSDK.IO.FileFormat.GLTFAscii
        'Detect the file format of the file to be imported            
        Dim filename = frmMain.OpenFileDialog1.FileName
        If Not pManager.IOPluginRegistry.DetectFileFormat(filename, fileformat) Then

            ' Unrecognizable file format.
            ' Try to fall back to SDK's native file format (an GLTF binary file).
            fileformat = pManager.IOPluginRegistry.NativeReaderFormat
        End If

        Dim importOptions = Skill.GLTFSDK.IO.GLTFStreamOptionsGLTFReader.Create(pManager, "")
        Dim importer As Skill.GLTFSDK.IO.GLTFImporter = Skill.GLTFSDK.IO.GLTFImporter.Create(pManager, "")

        importer.FileFormat = fileformat    ' get file format
        Dim imp_status As Boolean = importer.Initialize(filename)
        If Not imp_status Then
            MsgBox("Failed to open " + frmMain.OpenFileDialog1.FileName, MsgBoxStyle.Exclamation, "GLTF Load Error...")
            pManager.Destroy()
            GoTo outofhere
        End If
        If Not importer.IsGLTF Then
            MsgBox("Are you sure this is a GLTF file? " + vbCrLf + frmMain.OpenFileDialog1.FileName, MsgBoxStyle.Exclamation, "GLTF Load Error...")
            pManager.Destroy()
            GoTo outofhere
        End If
        importOptions.SetOption(Skill.GLTFSDK.IO.GLTFStreamOptionsGLTF.MATERIAL, True)
        importOptions.SetOption(Skill.GLTFSDK.IO.GLTFStreamOptionsGLTF.TEXTURE, True)
        importOptions.SetOption(Skill.GLTFSDK.IO.GLTFStreamOptionsGLTF.LINK, False)
        importOptions.SetOption(Skill.GLTFSDK.IO.GLTFStreamOptionsGLTF.SHAPE, False)
        importOptions.SetOption(Skill.GLTFSDK.IO.GLTFStreamOptionsGLTF.GOBO, False)
        importOptions.SetOption(Skill.GLTFSDK.IO.GLTFStreamOptionsGLTF.ANIMATION, False)
        importOptions.SetOption(Skill.GLTFSDK.IO.GLTFStreamOptionsGLTF.GLOBAL_SETTINGS, False)

        imp_status = importer.Import(scene, importOptions)

        Dim rootnode As GLTFNode = scene.RootNode

        Dim p As GLTFProperty = rootnode.GetFirstProperty

        Dim sc = rootnode.Scaling.GetValueAsDouble3
        'While 1
        '    'Debug.WriteLine(p.Name)
        '    p = rootnode.GetNextProperty(p)
        '    If Not p.IsValid Then Exit While
        'End While

        Dim tankComponentCount As Int32 = 0
        Dim TboneCount As Int32 = 0
        'make room for the mesh data
        Dim cnt As Integer = 0

        Dim childnode As GLTFNode
        Dim mesh As GLTFMesh = Nothing
        LOADING_GLTF = True ' so we dont read from the res_Mods folder
        Dim r_c As Integer = 0
        For i = 1 To rootnode.GetChildCount
            childnode = rootnode.GetChild(i - 1)
            Dim nCnt = childnode.GetChildCount
            If nCnt = 0 Then
                'Stop
            End If
            mesh = childnode.Mesh
            If mesh IsNot Nothing Then
                Dim at = childnode.Light
                Dim cam = childnode.Camera
                If cam Is Nothing Then

                    If at Is Nothing Then

                        If Not childnode.Name.ToLower.Contains("clone") Then
                            tankComponentCount += 1
                            readMeshdata(tankComponentCount, childnode, start_vertex, start_index, scene, rootnode, mesh)

                        End If
                    End If
                End If
            End If
            Dim child_Count = childnode.GetChildCount

        Next
        'clean up 
        importer.Destroy()
        rootnode.Destroy()
        pManager.Destroy()
        '        Try
        process_GLTF_data()
        For i = 1 To object_count - 1
            tank_center_X += _object(i).center_x
            tank_center_Y += _object(i).center_y
            tank_center_Z += _object(i).center_z
        Next
        tank_center_X /= object_count
        tank_center_Y /= object_count
        tank_center_Z /= object_count
        look_point_x = tank_center_X
        look_point_y = tank_center_Y
        look_point_z = tank_center_Z

        'Catch ex As Exception

        'End Try

outofhere:
        frmMain.info_Label.Text = "Creating Display Lists"
        Application.DoEvents()
        For i = 1 To GLTFgrp.Length - 1
            Dim id = Gl.glGenLists(1)
            Gl.glNewList(id, Gl.GL_COMPILE)
            GLTFgrp(i).call_list = id
            make_GLTF_display_lists(GLTFgrp(i).nPrimitives_ * 3, i)
            Gl.glEndList()
        Next
        GLTF_LOADED = True
        frmMain.info_Label.Visible = False
        frmMain.m_show_GLTF.Checked = True
        If MODEL_LOADED Then
            frmMain.m_show_GLTF.Visible = True
        End If
        LOADING_GLTF = False ' so we dont read from the res_Mods folder

    End Sub
    Public Sub import_primitives_glTF()
        'GLTF import sub
        Dim j As UInt32 = 0
        Dim i As UInt32 = 0
        Dim start_index As Integer = 0
        Dim start_vertex As Integer = 0
        Dim tfp As String = "C:\"
        If File.Exists(Temp_Storage + "\GLTF_Primi_in_folder.txt") Then
            tfp = File.ReadAllText(Temp_Storage + "\GLTF_Primi_in_folder.txt")
        End If
        frmMain.OpenFileDialog1.InitialDirectory = tfp
        frmMain.OpenFileDialog1.Filter = "AutoDesk (*.GLTF)|*.GLTF"
        frmMain.OpenFileDialog1.Title = "Import PRIMITIVES GLTF..."
        If frmMain.OpenFileDialog1.FileName = "OpenFileDialog1" Then
            frmMain.OpenFileDialog1.FileName = ""
        End If
        If Not frmMain.OpenFileDialog1.ShowDialog = Forms.DialogResult.OK Then
            If Not PRIMITIVES_MODE Then
                frmMain.m_load_textures.Enabled = True
            End If
            Return
        End If
        File.WriteAllText(Temp_Storage + "\GLTF_Primi_in_folder.txt", Path.GetDirectoryName(frmMain.OpenFileDialog1.FileName))
        ReDim UV2s(100000)
        uv2_total_count = 0

        frmComponentView.clear_GLTF_list()
        frmReverseVertexWinding.clear_group_list()

        My.Settings.GLTF_path = Path.GetDirectoryName(frmMain.OpenFileDialog1.FileName)
        frmMain.clean_house()
        remove_loaded_glTF()
        'frmMain.info_Label.Visible = True
        frmMain.info_Label.Text = frmMain.OpenFileDialog1.FileName
        Application.DoEvents()
        Application.DoEvents()
        Application.DoEvents()
        frmMain.pb1.Visible = True
        Application.DoEvents()

        Dim pManager As GLTFSdkManager
        Dim scene As GLTFScene
        pManager = GLTFSdkManager.Create
        scene = GLTFScene.Create(pManager, "My Scene")
        Dim fileformat As Integer = Skill.GLTFSDK.IO.FileFormat.GLTFAscii
        'Detect the file format of the file to be imported            
        Dim filename = frmMain.OpenFileDialog1.FileName
        If Not pManager.IOPluginRegistry.DetectFileFormat(filename, fileformat) Then
            ' Unrecognizable file format.
            ' Try to fall back to SDK's native file format (an GLTF binary file).
            fileformat = pManager.IOPluginRegistry.NativeReaderFormat
        End If

        Dim importOptions = Skill.GLTFSDK.IO.GLTFStreamOptionsGLTFReader.Create(pManager, "")
        Dim importer As Skill.GLTFSDK.IO.GLTFImporter = Skill.GLTFSDK.IO.GLTFImporter.Create(pManager, "")

        importer.FileFormat = fileformat    ' get file format
        Dim imp_status As Boolean = importer.Initialize(filename)
        If Not imp_status Then
            MsgBox("Failed to open " + frmMain.OpenFileDialog1.FileName, MsgBoxStyle.Exclamation, "GLTF Load Error...")
            pManager.Destroy()
            GoTo outofhere
        End If
        If Not importer.IsGLTF Then
            MsgBox("Are you sure this is a GLTF file? " + vbCrLf + frmMain.OpenFileDialog1.FileName, MsgBoxStyle.Exclamation, "GLTF Load Error...")
            pManager.Destroy()
            GoTo outofhere
        End If
        importOptions.SetOption(Skill.GLTFSDK.IO.GLTFStreamOptionsGLTF.MATERIAL, True)
        importOptions.SetOption(Skill.GLTFSDK.IO.GLTFStreamOptionsGLTF.TEXTURE, True)
        importOptions.SetOption(Skill.GLTFSDK.IO.GLTFStreamOptionsGLTF.LINK, False)
        importOptions.SetOption(Skill.GLTFSDK.IO.GLTFStreamOptionsGLTF.SHAPE, False)
        importOptions.SetOption(Skill.GLTFSDK.IO.GLTFStreamOptionsGLTF.GOBO, False)
        importOptions.SetOption(Skill.GLTFSDK.IO.GLTFStreamOptionsGLTF.ANIMATION, False)
        importOptions.SetOption(Skill.GLTFSDK.IO.GLTFStreamOptionsGLTF.GLOBAL_SETTINGS, False)

        imp_status = importer.Import(scene, importOptions)

        Dim rootnode As GLTFNode = scene.RootNode

        Dim p As GLTFProperty = rootnode.GetFirstProperty

        Dim sc = rootnode.Scaling.GetValueAsDouble3
        While 1
            p = rootnode.GetNextProperty(p)
            If Not p.IsValid Then Exit While
        End While

        Dim tankComponentCount As Int32 = 0
        Dim TboneCount As Int32 = 0
        'make room for the mesh data
        Dim cnt As Integer = 0

        Dim childnode As GLTFNode
        Dim mesh As GLTFMesh = Nothing
        LOADING_GLTF = True ' so we dont read from the res_Mods folder
        Dim r_c As Integer = 0
        For i = 1 To rootnode.GetChildCount
            childnode = rootnode.GetChild(i - 1)
            Dim nCnt = childnode.GetChildCount
            If nCnt = 0 Then
                'Stop
            End If
            mesh = childnode.Mesh
            If mesh IsNot Nothing Then
                Dim at = childnode.Light
                Dim cam = childnode.Camera
                If cam Is Nothing Then

                    If at Is Nothing Then

                        If Not childnode.Name.ToLower.Contains("clone") Then
                            tankComponentCount += 1
                            readMeshdata_primitives(tankComponentCount, childnode, start_vertex, start_index, scene, rootnode, mesh)

                        End If
                    End If
                End If
            End If
        Next
        'clean up 
        importer.Destroy()
        rootnode.Destroy()
        pManager.Destroy()

outofhere:
        frmMain.info_Label.Text = "Creating Display Lists"
        Application.DoEvents()
        frmComponentView.splitter.Panel1.Controls.Clear()
        ReDim m_groups(3)
        m_groups(2) = New mgrp_
        ReDim m_groups(2).list(GLTFgrp.Length)
        m_groups(2).cnt = GLTFgrp.Length - 1
        For i = 1 To GLTFgrp.Length - 1
            m_groups(2).list(i - 1) = i
            frmComponentView.add_to_GLTF_list(i, GLTFgrp(i).name)
            frmReverseVertexWinding.add_to_GLTF_list(i, GLTFgrp(i).name)
            Dim id = Gl.glGenLists(1)
            GLTFgrp(i).visible = True
            GLTFgrp(i).component_visible = True
            GLTFgrp(i).reverse_winding = False
            Gl.glNewList(id, Gl.GL_COMPILE)
            GLTFgrp(i).call_list = id
            make_GLTF_display_lists(GLTFgrp(i).nPrimitives_ * 3, i)
            Gl.glEndList()
        Next
        m_groups(2).changed = True
        m_groups(2).new_objects = False
        m_groups(2).m_type = 2 'hull no flipping or anything
        GLTF_LOADED = True
        MODEL_LOADED = True
        frmMain.info_Label.Visible = False
        frmMain.m_show_GLTF.Checked = True
        frmMain.m_show_GLTF.Visible = True
        frmMain.m_show_GLTF.Checked = True
        frmMain.m_show_GLTF.Enabled = False
        frmMain.m_hide_show_components.Enabled = True
        frmMain.m_set_vertex_winding_order.Enabled = True
        LOADING_GLTF = False ' so we dont read from the res_Mods folder
        PRIMITIVES_MODE = True
        frmMain.m_write_non_tank_primitive.Enabled = True
    End Sub

    Public Sub export_glTF()
        'export GLTF
        Dim rootNode As GLTFNode
        Dim id As Integer
        Dim model_name As String = ""
        Dim mat_main As String = ""
        Dim mat_NM As String = ""
        Dim mat_uv2 As String = ""
        Dim GLTF_locaction As String = My.Settings.GLTF_path
        Dim rp As String = Application.StartupPath
        Dim _date As String = Date.Now
        Dim ar = _date.Split(" ")
        _date = ar(0) + " " + ar(1) + ".0"


        Dim vert_string, normal_string, uv1_string, uv2_string, uv_index, indices_string As New StringBuilder

        'Tried everything so lets do it the hard way
        '--------------------------------------------------------------------------
        Dim m_name As String = "Material"
        Dim s_name As String = "Phong"
        Dim EmissiveColor = New Vector3(0.0, 0.0, 0.0)
        Dim AmbientColor = New Vector3(0.9, 0.9, 0.9)
        Dim SpecularColor = New Vector3(0.7, 0.7, 0.7)
        Dim DiffuseColor As New Vector3(0.8, 0.8, 0.8)
        '--------------------------------------------------------------------------

        'create the material and texture arrays.

        Dim texture_count = textures.Length
        Dim lMaterials(texture_count) As PbrMaterial
        Dim lTextures(texture_count) As Texture
        Dim lTextures_N(texture_count) As Texture
        'make the materials
        For i = 0 To texture_count - 1
            lMaterials(i) = GLTF_create_material(i) 'Material
            lTextures(i) = GLTF_create_texture(i) 'Color Map
            lTextures_N(i) = GLTF_create_texture_N(i) 'Normal Map
        Next
        'make v materials

        'create manager and scene
        Dim scene As GLTFScene
        scene = GLTFScene.Create(pManager, file_name)
        scene.SceneInfo.Author = "Exported using Coffee_'s Tank Exporter tool"
        scene.SceneInfo.Comment = TANK_NAME

        frmGLTF.Label1.Visible = False
        Dim node_list() = {GLTFNode.Create(pManager, model_name)}
        Dim node_Vlist() = {GLTFNode.Create(pManager, "pin")}
        '--------------------------------------------------------------------------
        rootNode = scene.RootNode
        rootNode.CreateTakeNode("Show all faces")
        rootNode.SetCurrentTakeNode("Show all faces")


        Dim dfr = New GLTFVector4(0.0, 0.0, 0.0, 0.0)
        Dim dfs = New GLTFVector4(1.0, 1.0, 1.0, 0.0)
        Dim dft As New GLTFVector4(0.0, 0.0, 0.0, 1.0)
        rootNode.SetDefaultR(dfr)
        rootNode.SetDefaultS(dfs)
        rootNode.SetDefaultT(dft)
        'add the markers to the root
        ' get total vNodes needed
        Dim cnt As Integer = 0



        For id = 1 To object_count
            ReDim Preserve node_list(id + 1)

            mat_main = GLTF_Texture_path + "\" + Path.GetFileNameWithoutExtension(_group(id).color_name) + ".png"
            mat_NM = GLTF_Texture_path + "\" + Path.GetFileNameWithoutExtension(_group(id).normal_name) + ".png"
            mat_uv2 = _group(id).detail_name

            model_name = _group(id).name.Replace("/", "\")
            model_name = model_name.Replace(":", "~")
            model_name = model_name.Replace("vehicles\", "")
            model_name = model_name.Replace("primitives_processed", "pri")
            model_name = model_name.Replace("\lod0\", "\l\")
            node_list(id) = GLTFNode.Create(pManager, model_name)

            'create mesh node
            Dim mymesh = GLTF_create_mesh(model_name, id, pManager)

            'Dim m As New GLTFXMatrix
            Dim m_ = _object(id).matrix

            Dim scale As New SharpDX.Vector3
            Dim rot As New SharpDX.Quaternion
            Dim trans As New SharpDX.Vector3
            Dim Mt As New SharpDX.Matrix
            Mt = load_matrix_decompose(m_, trans, scale, rot)

            Dim r_vector As New GLTFVector4(rot.X, 0.0, rot.Z, rot.W)
            Dim t_vector As New GLTFVector4(-trans.X, trans.Y, trans.Z)
            Dim s_vector As New GLTFVector4(-scale.X, scale.Y, scale.Z, 0.0)

            If model_name.ToLower.Contains("chassis") Then
                s_vector.Z *= -1.0
                s_vector.X *= -1.0
            End If
            If id = object_count And Not CRASH_MODE Then
                s_vector.Z *= -1.0
                s_vector.X *= -1.0
            End If

            'Need a layercontainer to put the texture in.
            'dont add the textures if we are not exporting them!
            Dim layercontainer As GLTFLayerContainer = mymesh
            Dim layerElementTexture As GLTFLayerElementTexture = layercontainer.GetLayer(0).DiffuseTextures
            Dim layerElementNTexture As GLTFLayerElementTexture = layercontainer.GetLayer(0).BumpTextures
            If layerElementTexture Is Nothing Then
                layerElementTexture = GLTFLayerElementTexture.Create(layercontainer, "diffuseMap")
                layercontainer.GetLayer(0).DiffuseTextures = layerElementTexture
                layerElementNTexture = GLTFLayerElementTexture.Create(layercontainer, "normalMap")
                layercontainer.GetLayer(0).BumpTextures = layerElementNTexture
            End If
            'not 100% sure about the translucent but it isn't breaking anything.
            layerElementTexture.Blend_Mode = GLTFLayerElementTexture.BlendMode.Translucent
            layerElementTexture.Alpha = 1.0
            layerElementTexture.Mapping_Mode = GLTFLayerElement.MappingMode.AllSame
            layerElementTexture.Reference_Mode = GLTFLayerElement.ReferenceMode.Direct

            layerElementNTexture.Blend_Mode = GLTFLayerElementTexture.BlendMode.Translucent
            layerElementNTexture.Alpha = 1.0
            layerElementNTexture.Mapping_Mode = GLTFLayerElement.MappingMode.AllSame
            layerElementNTexture.Reference_Mode = GLTFLayerElement.ReferenceMode.Direct

            'add the texture from the texture array using the Texture ID for this mesh section
            layerElementTexture.DirectArray.Add(lTextures(_group(id).texture_id))
            layerElementNTexture.DirectArray.Add(lTextures_N(_group(id).texture_id))
            node_list(id).NodeAttribute = mymesh
            Dim dr, ds, dt As New GLTFVector4
            dr.Set(0, 0, 0, 0)
            ds.Set(1, 1, 1, 1)
            dt.Set(0, 0, 0, 1)

            node_list(id).SetDefaultR(r_vector)
            node_list(id).SetDefaultT(t_vector)
            node_list(id).SetDefaultS(s_vector)

            If node_list(id).IsValid And frmGLTF.export_textures.Checked Then ' useless test but Im leaving it.
                'add the texture from the array using this models texture ID
                node_list(id).AddMaterial(lMaterials(_group(id).texture_id))
                '---------------------------------------
                'If we dont connect this texture to this node, it will never show up!
                node_list(id).ConnectSrcObject(lMaterials(_group(id).texture_id), GLTFConnectionType.ConnectionDefault)
            End If
            node_list(id).Shading_Mode = GLTFNode.ShadingMode.TextureShading ' not even sure this is needed but what ever.
            Dim estr = pManager.LastErrorString
            Dim vstr = mymesh.LastErrorString
            Dim vmm = node_list(id).LastErrorString

            rootNode.AddChild(node_list(id))
            rootNode.ConnectSrcObject(node_list(id), GLTFConnectionType.ConnectionDefault)

we_dont_want_this_one_saved:
        Next 'Id

        'time to save... not sure im even close to having what i need to save but fuck it!
        Dim exporter As Skill.GLTFSDK.IO.GLTFExporter = GLTFExporter.Create(pManager, "")
        If Not exporter.Initialize(frmMain.SaveFileDialog1.FileName) Then
            MsgBox("GLTF unable to initialize exporter!", MsgBoxStyle.Exclamation, "GLTF Error..")
            GoTo outahere
        End If
        Dim version As Version = Skill.GLTFSDK.IO.GLTFIO.CurrentVersion
        Console.Write(String.Format("GLTF version number for this GLTF SDK is {0}.{1}.{2}",
                          version.Major, version.Minor, version.Revision))
        If frmGLTF.export_as_binary_cb.Checked Then
            exporter.FileFormat = IO.FileFormat.GLTFBinary
        Else
            exporter.FileFormat = IO.FileFormat.GLTFAscii
        End If

        Dim exportOptions As Skill.GLTFSDK.IO.GLTFStreamOptionsGLTFWriter _
                = Skill.GLTFSDK.IO.GLTFStreamOptionsGLTFWriter.Create(pManager, "")
        If pManager.IOPluginRegistry.WriterIsGLTF(IO.FileFormat.GLTFAscii) Then

            ' Export options determine what kind of data is to be imported.
            ' The default (except for the option eEXPORT_TEXTURE_AS_EMBEDDED)
            ' is true, but here we set the options explictly.
            exportOptions.SetOption(Skill.GLTFSDK.IO.GLTFStreamOptionsGLTF.MATERIAL, True)
            exportOptions.SetOption(Skill.GLTFSDK.IO.GLTFStreamOptionsGLTF.TEXTURE, True)
            exportOptions.SetOption(Skill.GLTFSDK.IO.GLTFStreamOptionsGLTF.EMBEDDED, False)
            exportOptions.SetOption(Skill.GLTFSDK.IO.GLTFStreamOptionsGLTF.LINK, True)
            exportOptions.SetOption(Skill.GLTFSDK.IO.GLTFStreamOptionsGLTF.SHAPE, False)
            exportOptions.SetOption(Skill.GLTFSDK.IO.GLTFStreamOptionsGLTF.GOBO, False)
            exportOptions.SetOption(Skill.GLTFSDK.IO.GLTFStreamOptionsGLTF.ANIMATION, False)
            exportOptions.SetOption(Skill.GLTFSDK.IO.GLTFStreamOptionsGLTF.GLOBAL_SETTINGS, False)
            exportOptions.SetOption(Skill.GLTFSDK.IO.GLTFStreamOptionsGLTF.MEDIA, False)
        End If
        Dim status = exporter.Export(scene, exportOptions)
        exporter.Destroy()
        pManager.Destroy()

outahere:
        frmGLTF.Label1.Visible = True

    End Sub
    Public Sub export_primitives_GLTF()
        'export GLTFprimitives_
        Dim rootNode As GLTFNode
        Dim id As Integer
        Dim model_name As String = ""
        Dim mat_main As String = ""
        Dim mat_NM As String = ""
        Dim mat_uv2 As String = ""
        Dim GLTF_locaction As String = My.Settings.GLTF_path
        Dim rp As String = Application.StartupPath
        Dim _date As String = Date.Now
        Dim ar = _date.Split(" ")
        _date = ar(0) + " " + ar(1) + ".0"


        Dim vert_string, normal_string, uv1_string, uv2_string, uv_index, indices_string As New StringBuilder

        'Tried everything so lets do it the hard way
        '--------------------------------------------------------------------------
        Dim pManager As GLTFSdkManager
        pManager = GLTFSdkManager.Create
        'create the material and texture arrays.

        Dim texture_count = object_count
        Dim lMaterials(texture_count) As GLTFSurfacePhong
        Dim lTextures(texture_count) As GLTFTexture
        Dim lTextures_N(texture_count) As GLTFTexture
        'make the materials
        For i = 1 To texture_count
            lMaterials(i) = GLTF_create_material(pManager, i) 'Material
            lTextures(i) = GLTF_create_texture_primtive(pManager, i) 'Color Map
            lTextures_N(i) = GLTF_create_texture_N_primitive(pManager, i) 'Normal Map
        Next
        'create manager and scene
        Dim scene As GLTFScene
        scene = GLTFScene.Create(pManager, file_name)
        scene.SceneInfo.Author = "Exported using Coffee_'s Tank Exporter tool"
        scene.SceneInfo.Comment = TANK_NAME
        'scene.CreateTake("Show all faces")
        'scene.SetCurrentTake("Show all faces")

        frmGLTF.Label1.Visible = False
        Dim node_list() = {GLTFNode.Create(pManager, model_name)}
        Dim node_Vlist() = {GLTFNode.Create(pManager, "pin")}
        '--------------------------------------------------------------------------
        rootNode = scene.RootNode
        rootNode.CreateTakeNode("Show all faces")
        rootNode.SetCurrentTakeNode("Show all faces")


        Dim dfr = New GLTFVector4(0.0, 0.0, 0.0, 0.0)
        Dim dfs = New GLTFVector4(1.0, 1.0, 1.0, 0.0)
        Dim dft As New GLTFVector4(0.0, 0.0, 0.0, 1.0)
        rootNode.SetDefaultR(dfr)
        rootNode.SetDefaultS(dfs)
        rootNode.SetDefaultT(dft)
        'add the markers to the root
        ' get total vNodes needed
        Dim cnt As Integer = 0

        For id = 1 To object_count
            ReDim Preserve node_list(id + 1)

            model_name = GLTF_NAME + "~" + id.ToString
            node_list(id) = GLTFNode.Create(pManager, model_name)
            'create mesh node
            Dim mymesh = GLTF_create_primi_mesh(model_name, id, pManager)

            Dim m_ = _object(id).matrix

            Dim scale As New SharpDX.Vector3
            Dim rot As New SharpDX.Quaternion
            Dim trans As New SharpDX.Vector3
            Dim Mt As New SharpDX.Matrix
            Mt = load_matrix_decompose(m_, trans, scale, rot)


            Dim r_vector As New GLTFVector4(rot.X, 0.0, rot.Z, rot.W)
            Dim t_vector As New GLTFVector4(-trans.X, trans.Y, trans.Z)
            Dim s_vector As New GLTFVector4(-scale.X, scale.Y, scale.Z, 0.0)

            Dim layercontainer As GLTFLayerContainer = mymesh

            If _group(id).is_atlas_type Then
            End If

            Dim layerElementTexture As GLTFLayerElementTexture = layercontainer.GetLayer(0).DiffuseTextures
            Dim layerElementNTexture As GLTFLayerElementTexture = layercontainer.GetLayer(0).BumpTextures

            If layerElementTexture Is Nothing Then
                layerElementTexture = GLTFLayerElementTexture.Create(layercontainer, "diffuseMap")
                layercontainer.GetLayer(0).DiffuseTextures = layerElementTexture
                layerElementNTexture = GLTFLayerElementTexture.Create(layercontainer, "normalMap")
                layercontainer.GetLayer(0).BumpTextures = layerElementNTexture
            End If

            'not 100% sure about the translucent but it isn't breaking anything.
            layerElementTexture.Blend_Mode = GLTFLayerElementTexture.BlendMode.Translucent
            layerElementTexture.Alpha = 1.0
            layerElementTexture.Mapping_Mode = GLTFLayerElement.MappingMode.AllSame
            layerElementTexture.Reference_Mode = GLTFLayerElement.ReferenceMode.Direct

            layerElementNTexture.Blend_Mode = GLTFLayerElementTexture.BlendMode.Translucent
            layerElementNTexture.Alpha = 1.0
            layerElementNTexture.Mapping_Mode = GLTFLayerElement.MappingMode.AllSame
            layerElementNTexture.Reference_Mode = GLTFLayerElement.ReferenceMode.Direct

            'add the texture from the texture array using the Texture ID for this mesh section
            layerElementTexture.DirectArray.Add(lTextures(_group(id).GLTF_texture_id))
            layerElementNTexture.DirectArray.Add(lTextures_N(_group(id).GLTF_N_texture_id))
            'add the texture from the texture array using the Texture ID for this mesh section
            node_list(id).NodeAttribute = mymesh
            Dim dr, ds, dt As New GLTFVector4
            dr.Set(0, 0, 0, 0)
            ds.Set(1, 1, 1, 1)
            dt.Set(0, 0, 0, 1)
            node_list(id).SetDefaultR(r_vector)
            node_list(id).SetDefaultT(t_vector)
            node_list(id).SetDefaultS(s_vector)

            If node_list(id).IsValid And frmGLTF.export_textures.Checked Then ' useless test but Im leaving it.
                'add the texture from the array using this models texture ID
                Try
                    node_list(id).AddMaterial(lMaterials(_group(id).texture_id))

                    '---------------------------------------
                    'If we dont connect this texture to this node, it will never show up!
                    node_list(id).ConnectSrcObject(lMaterials(id), GLTFConnectionType.ConnectionDefault)
                Catch ex As Exception

                End Try
            End If
            node_list(id).Shading_Mode = GLTFNode.ShadingMode.TextureShading ' not even sure this is needed but what ever.
            Dim estr = pManager.LastErrorString
            Dim vstr = mymesh.LastErrorString
            Dim vmm = node_list(id).LastErrorString
            'Debug.WriteLine(id.ToString("000") + ":--------")
            'Debug.WriteLine(estr)
            'Debug.WriteLine(vstr)
            'Debug.WriteLine(vmm)

            rootNode.AddChild(node_list(id))
            rootNode.ConnectSrcObject(node_list(id), GLTFConnectionType.ConnectionDefault)

we_dont_want_this_one_saved:
        Next 'Id



        'time to save... not sure im even close to having what i need to save but fuck it!
        Dim exporter As Skill.GLTFSDK.IO.GLTFExporter = GLTFExporter.Create(pManager, "")
        If Not exporter.Initialize(frmMain.SaveFileDialog1.FileName) Then
            MsgBox("GLTF unable to initialize exporter!", MsgBoxStyle.Exclamation, "GLTF Error..")
            GoTo outahere
        End If
        Dim version As Version = Skill.GLTFSDK.IO.GLTFIO.CurrentVersion
        Console.Write(String.Format("GLTF version number for this GLTF SDK is {0}.{1}.{2}",
                          version.Major, version.Minor, version.Revision))
        If frmGLTF.export_as_binary_cb.Checked Then
            exporter.FileFormat = IO.FileFormat.GLTFBinary
        Else
            exporter.FileFormat = IO.FileFormat.GLTFAscii
        End If

        Dim exportOptions As Skill.GLTFSDK.IO.GLTFStreamOptionsGLTFWriter _
                = Skill.GLTFSDK.IO.GLTFStreamOptionsGLTFWriter.Create(pManager, "")
        If pManager.IOPluginRegistry.WriterIsGLTF(IO.FileFormat.GLTFAscii) Then

            ' Export options determine what kind of data is to be imported.
            ' The default (except for the option eEXPORT_TEXTURE_AS_EMBEDDED)
            ' is true, but here we set the options explictly.
            exportOptions.SetOption(Skill.GLTFSDK.IO.GLTFStreamOptionsGLTF.MATERIAL, True)
            exportOptions.SetOption(Skill.GLTFSDK.IO.GLTFStreamOptionsGLTF.TEXTURE, True)
            exportOptions.SetOption(Skill.GLTFSDK.IO.GLTFStreamOptionsGLTF.EMBEDDED, False)
            exportOptions.SetOption(Skill.GLTFSDK.IO.GLTFStreamOptionsGLTF.LINK, True)
            exportOptions.SetOption(Skill.GLTFSDK.IO.GLTFStreamOptionsGLTF.SHAPE, False)
            exportOptions.SetOption(Skill.GLTFSDK.IO.GLTFStreamOptionsGLTF.GOBO, False)
            exportOptions.SetOption(Skill.GLTFSDK.IO.GLTFStreamOptionsGLTF.ANIMATION, False)
            exportOptions.SetOption(Skill.GLTFSDK.IO.GLTFStreamOptionsGLTF.GLOBAL_SETTINGS, False)
            exportOptions.SetOption(Skill.GLTFSDK.IO.GLTFStreamOptionsGLTF.MEDIA, False)
        End If
        Dim status = exporter.Export(scene, exportOptions)
        exporter.Destroy()
        pManager.Destroy()
        'textureAmbientLayer.Destroy()
        'textureDiffuseLayer.Destroy()
outahere:
        frmGLTF.Label1.Visible = True

    End Sub

#Region "Import helpers"


    Private Function readMeshdata(ByVal i As Integer, ByRef childnode As GLTFNode,
                                  start_vertex As Integer, start_index As Integer,
                                  scene As GLTFScene, rootnode As GLTFNode, mesh As GLTFMesh)

        ReDim Preserve GLTFgrp(i)

        GLTFgrp(i).name = childnode.NameOnly
        If Not GLTFgrp(i).name.Contains("vehicles\") And (GLTFgrp(i).name.Contains("\l\") Or GLTFgrp(i).name.Contains("\lod0\")) Then
            GLTFgrp(i).name = "vehicles\" + childnode.NameOnly
            GLTFgrp(i).name = GLTFgrp(i).name.Replace("pri~", "primitives_processed~")
            GLTFgrp(i).name = GLTFgrp(i).name.Replace("primitives~", "primitives_processed~")
            GLTFgrp(i).name = GLTFgrp(i).name.Replace("\l\", "\lod0\")
        End If
        'get transform information -------------------------------------
        Dim GLTF_matrix As New GLTFXMatrix
        GLTFgrp(i).rotation = New GLTFVector4
        GLTFgrp(i).translation = New GLTFVector4
        GLTFgrp(i).scale = New GLTFVector4
        GLTFgrp(i).scale.X = 1.0
        GLTFgrp(i).scale.Y = 1.0
        GLTFgrp(i).scale.Z = 1.0

        Dim t As New GLTFTime
        Dim GlobalUnitScale = scene.GlobalSettings.FindProperty("UnitScaleFactor", False).GetValueAsDouble

        Dim ls = childnode.GetLocalSFromDefaultTake(GLTFNode.PivotSet.SourceSet)
        If ls.X = 1.0 Then
            ls.X = 0.1
            ls.Y = 0.1
            ls.Z = 1.0
        End If

        Dim nodeGT = rootnode.GetGlobalFromDefaultTake(GLTFNode.PivotSet.DestinationSet)

        Dim lr = childnode.GetLocalRFromDefaultTake(GLTFNode.PivotSet.SourceSet)
        Dim lt = childnode.GetLocalTFromCurrentTake(t)
        Dim gr = childnode.Parent.GetLocalRFromCurrentTake(t)

        Dim scaling = childnode.Scaling.GetValueAsDouble3

        GLTFgrp(i).rotation = childnode.GetGeometricRotation(GLTFNode.PivotSet.SourceSet)
        GLTFgrp(i).translation = childnode.GetGeometricTranslation(GLTFNode.PivotSet.SourceSet)
        GLTFgrp(i).scale = childnode.GetGeometricScaling(GLTFNode.PivotSet.SourceSet)
        GLTF_matrix.SetIdentity()

        Dim dr As New GLTFVector4
        Dim dt As New GLTFVector4
        Dim ds As New GLTFVector4

        Dim gm = childnode.GetGlobalFromCurrentTake(t)

        childnode.GetDefaultR(dr)
        childnode.GetDefaultS(ds)
        childnode.GetDefaultT(dt)
        GLTFgrp(i).rotation = childnode.GetGeometricRotation(GLTFNode.PivotSet.SourceSet)
        GLTFgrp(i).translation = childnode.GetGeometricTranslation(GLTFNode.PivotSet.SourceSet)
        GLTFgrp(i).scale = childnode.GetGeometricScaling(GLTFNode.PivotSet.SourceSet)
        Dim TnR As Double = 0
        Try
            TnR = Round(GLTFgrp(i).rotation.X, 6) + Round(GLTFgrp(i).rotation.Y, 6) + Round(GLTFgrp(i).rotation.Z, 6) _
                + Round(GLTFgrp(i).translation.X, 6) + Round(GLTFgrp(i).translation.Y, 6) + Round(GLTFgrp(i).translation.Z, 6)
        Catch ex As Exception

        End Try

        GLTF_matrix.SetTRS(lt, lr, ds)
        GLTF_matrix = gm
        GLTF_matrix.Transpose()

        build_GLTF_matrix(i, GLTF_matrix)

        '---------------------------------------------------------------
        Dim mat_cnt As Integer = mesh.Node.GetSrcObjectCount(GLTFSurfaceMaterial.ClassId)
        Dim material As GLTFSurfaceMaterial = mesh.Node.GetSrcObject(GLTFSurfaceMaterial.ClassId, 0)
        Dim property_ As GLTFProperty = Nothing
        Dim texture As GLTFTexture
        'we never read a Ambient texture. Only Diffuse and Bump....
        Dim uv_scaling, uv_offset As New GLTFVector2
        GLTFgrp(i).texture_count = 1
        Try
            'diffuse texture.. color_name
            property_ = material.FindProperty(GLTFSurfaceMaterial.SDiffuse)
            If property_ IsNot Nothing Then

                texture = property_.GetSrcObject(GLTFTexture.ClassId, 0)
                uv_offset.X = texture.TranslationU
                uv_offset.Y = texture.TranslationV
                uv_scaling.X = texture.ScaleU
                uv_scaling.Y = texture.ScaleV

                Dim fp = Path.GetDirectoryName(frmMain.OpenFileDialog1.FileName) + "\" + texture.RelativeFileName
                GLTFgrp(i).color_name = fix_texture_path(fp)
                GLTFgrp(i).color_Id = -1
                frmMain.info_Label.Text = "Loading Texture: " + GLTFgrp(i).color_name
                Application.DoEvents()
                GLTFgrp(i).color_Id = get_GLTF_texture(GLTFgrp(i).color_name)
            Else
                GLTFgrp(i).color_Id = white_id

            End If
            If GLTFgrp(i).color_Id = 0 Then
                GLTFgrp(i).color_Id = white_id
            End If
        Catch ex As Exception
            GLTFgrp(i).color_Id = white_id
        End Try

        Try
            'normal map... normal_name
            property_ = material.FindProperty(GLTFSurfaceMaterial.SBump)
            texture = property_.GetSrcObject(GLTFTexture.ClassId, 0)
            If texture IsNot Nothing Then
                Dim fp = Path.GetDirectoryName(frmMain.OpenFileDialog1.FileName) + "\" + texture.RelativeFileName
                GLTFgrp(i).normal_name = fix_texture_path(fp)
                frmMain.info_Label.Text = "Loading Texture: " + GLTFgrp(i).normal_name
                Application.DoEvents()
                GLTFgrp(i).normal_Id = -1
                GLTFgrp(i).normal_Id = get_GLTF_texture(GLTFgrp(i).normal_name)
                GLTFgrp(i).bumped = True
                GLTFgrp(i).texture_count = 2
            Else
                property_ = material.FindProperty(GLTFSurfaceMaterial.SNormalMap)
                texture = property_.GetSrcObject(GLTFTexture.ClassId, 0)
                If texture IsNot Nothing Then
                    Dim fp = Path.GetDirectoryName(frmMain.OpenFileDialog1.FileName) + "\" + texture.RelativeFileName
                    GLTFgrp(i).normal_name = fix_texture_path(fp)
                    frmMain.info_Label.Text = "Loading Texture: " + GLTFgrp(i).normal_name
                    Application.DoEvents()
                    GLTFgrp(i).normal_Id = -1
                    GLTFgrp(i).normal_Id = get_GLTF_texture(GLTFgrp(i).normal_name)
                    GLTFgrp(i).bumped = True
                    GLTFgrp(i).texture_count = 2

                Else
                    Dim texture_n = GLTFgrp(i).color_name.Replace("AM", "ANM")
                    If File.Exists(texture_n) Then
                        GLTFgrp(i).normal_name = texture_n
                        frmMain.info_Label.Text = "Loading Texture: " + texture_n
                        Application.DoEvents()
                        GLTFgrp(i).normal_Id = -1
                        GLTFgrp(i).normal_Id = get_GLTF_texture(texture_n)
                        GLTFgrp(i).bumped = True
                        GLTFgrp(i).texture_count = 2
                    Else
                        GLTFgrp(i).bumped = False
                        GLTFgrp(i).texture_count = 1
                    End If

                End If


            End If
        Catch ex As Exception
            GLTFgrp(i).normal_Id = white_id

        End Try

        ' WG has made all simple shaders useless in the tanks visual files.
        ' All shaders must now be PBS_Tank or PBS_Tank_Skinned
        ' There is no use for specular textures in PBS shaders.
        'Try
        '    'specular map... specular_name
        '    property_ = material.FindProperty(GLTFSurfaceMaterial.SSpecularFactor)
        '    texture = property_.GetSrcObject(GLTFTexture.ClassId, 0)
        '    If texture IsNot Nothing Then
        '        Dim fp = Path.GetDirectoryName(frmMain.OpenFileDialog1.FileName) + "\" + texture.RelativeFileName
        '        GLTFgrp(i).specular_name = fix_texture_path(fp)
        '        frmMain.info_Label.Text = "Loading Texture: " + GLTFgrp(i).specular_name
        '        Application.DoEvents()
        '        GLTFgrp(i).specular_id = -1
        '        GLTFgrp(i).specular_id = get_GLTF_texture(GLTFgrp(i).specular_name)
        '        GLTFgrp(i).texture_count = 3
        '        If GLTFgrp(i).specular_id = 0 Then
        '            GLTFgrp(i).specular_id = white_id
        '        End If
        '    End If
        'Catch ex As Exception

        'End Try
        Return get_mesh_geo(i, childnode, start_vertex, start_index, scene, rootnode, mesh)
    End Function
    Private Function readMeshdata_primitives(ByVal i As Integer, ByRef childnode As GLTFNode,
                                start_vertex As Integer, start_index As Integer,
                                scene As GLTFScene, rootnode As GLTFNode, mesh As GLTFMesh)

        ReDim Preserve GLTFgrp(i)


        GLTFgrp(i).name = childnode.NameOnly
        'get transform information -------------------------------------
        Dim GLTF_matrix As New GLTFXMatrix
        GLTFgrp(i).rotation = New GLTFVector4
        GLTFgrp(i).translation = New GLTFVector4
        GLTFgrp(i).scale = New GLTFVector4
        GLTFgrp(i).scale.X = 1.0
        GLTFgrp(i).scale.Y = 1.0
        GLTFgrp(i).scale.Z = 1.0

        Dim t As New GLTFTime
        Dim GlobalUnitScale = scene.GlobalSettings.FindProperty("UnitScaleFactor", False).GetValueAsDouble

        Dim ls = childnode.GetLocalSFromDefaultTake(GLTFNode.PivotSet.SourceSet)
        If ls.X = 1.0 Then
            ls.X = 0.1
            ls.Y = 0.1
            ls.Z = 1.0
        End If

        Dim nodeGT = rootnode.GetGlobalFromDefaultTake(GLTFNode.PivotSet.DestinationSet)

        Dim lr = childnode.GetLocalRFromDefaultTake(GLTFNode.PivotSet.SourceSet)
        Dim lt = childnode.GetLocalTFromCurrentTake(t)
        Dim gr = childnode.Parent.GetLocalRFromCurrentTake(t)

        Dim scaling = childnode.Scaling.GetValueAsDouble3

        GLTFgrp(i).rotation = childnode.GetGeometricRotation(GLTFNode.PivotSet.SourceSet)
        GLTFgrp(i).translation = childnode.GetGeometricTranslation(GLTFNode.PivotSet.SourceSet)
        GLTFgrp(i).scale = childnode.GetGeometricScaling(GLTFNode.PivotSet.SourceSet)
        GLTF_matrix.SetIdentity()

        Dim dr As New GLTFVector4
        Dim dt As New GLTFVector4
        Dim ds As New GLTFVector4

        Dim gm = childnode.GetGlobalFromCurrentTake(t)

        childnode.GetDefaultR(dr)
        childnode.GetDefaultS(ds)
        childnode.GetDefaultT(dt)
        GLTFgrp(i).rotation = childnode.GetGeometricRotation(GLTFNode.PivotSet.SourceSet)
        GLTFgrp(i).translation = childnode.GetGeometricTranslation(GLTFNode.PivotSet.SourceSet)
        GLTFgrp(i).scale = childnode.GetGeometricScaling(GLTFNode.PivotSet.SourceSet)
        Dim TnR As Double = 0
        Try
            TnR = Round(GLTFgrp(i).rotation.X, 6) + Round(GLTFgrp(i).rotation.Y, 6) + Round(GLTFgrp(i).rotation.Z, 6) _
                + Round(GLTFgrp(i).translation.X, 6) + Round(GLTFgrp(i).translation.Y, 6) + Round(GLTFgrp(i).translation.Z, 6)
        Catch ex As Exception

        End Try

        GLTF_matrix.SetTRS(lt, lr, ds)
        GLTF_matrix = gm
        GLTF_matrix.Transpose()

        build_GLTF_matrix(i, GLTF_matrix)
        '---------------------------------------------------------------
        Dim mat_cnt As Integer = mesh.Node.GetSrcObjectCount(GLTFSurfaceMaterial.ClassId)
        Dim material As GLTFSurfaceMaterial = mesh.Node.GetSrcObject(GLTFSurfaceMaterial.ClassId, 0)
        Return get_mesh_geo(i, childnode, start_vertex, start_index, scene, rootnode, mesh)
    End Function
    Private Function get_mesh_geo(ByVal GLTF_idx As Integer, ByRef childnode As GLTFNode,
                                    start_vertex As Integer, start_index As Integer,
                                    scene As GLTFScene, rootnode As GLTFNode, mesh As GLTFMesh)

        Dim uvlayer1 As GLTFLayerElementUV = mesh.GetLayer(0).GetUVs
        Dim property_ As GLTFProperty = Nothing
        Dim texture As GLTFTexture
        Dim material As GLTFSurfaceMaterial = mesh.Node.GetSrcObject(GLTFSurfaceMaterial.ClassId, 0)
        Dim nVertices = mesh.Normals.Count
        '###############################################
        Dim index_mode = uvlayer1.Reference_Mode

        Dim eNormals As GLTFLayerElementNormal = mesh.GetLayer(0).Normals
        Dim uv2_Layer As GLTFLayerElementUV = Nothing
        If mesh.UVLayerCount = 2 Then
            property_ = material.FindProperty(GLTFSurfaceMaterial.SSpecularFactor)
            texture = property_.GetSrcObject(GLTFTexture.ClassId, 0)
            If texture Is Nothing Then
                uv2_Layer = mesh.GetLayer(1).GetUVs
                'Stop
            End If
        End If
        Dim cp_cnt As UInt32 = mesh.ControlPoints.Length
        Dim polycnt = mesh.PolygonCount
        Dim uvCount As UInt32 = uvlayer1.IndexArray.Count / 3
        GLTFgrp(GLTF_idx).nPrimitives_ = polycnt
        GLTFgrp(GLTF_idx).nVertices_ = nVertices
        GLTFgrp(GLTF_idx).startIndex_ = start_index : start_index += polycnt * 3
        GLTFgrp(GLTF_idx).startVertex_ = start_vertex : start_vertex += nVertices * 40

        ReDim GLTFgrp(GLTF_idx).cPoints(cp_cnt)
        mesh.ControlPoints.CopyTo(GLTFgrp(GLTF_idx).cPoints, 0)
        ReDim Preserve GLTFgrp(GLTF_idx).vertices(polycnt * 3)
        ReDim Preserve GLTFgrp(GLTF_idx).indicies(polycnt * 3)
        Dim vertexId As Integer = 0
        For k = 0 To polycnt * 3 - 1
            GLTFgrp(GLTF_idx).vertices(k) = New vertice_
            GLTFgrp(GLTF_idx).indicies(k) = New uvect3
        Next

        Dim colorLayer1 As GLTFLayerElementVertexColor = mesh.GetLayer(0).VertexColors
        Dim normal_layer = mesh.GetLayer(0).Normals
        Dim uv_layer = mesh.GetLayer(0).DiffuseUV


        Dim max_cp_index As Integer
        For i = 0 To polycnt - 1
            Dim pv_cnt As Integer = mesh.GetPolygonSize(0)
            If pv_cnt < 3 Or pv_cnt > 3 Then
                MsgBox("Your mesh is not made of triangles! ID:" + GLTFgrp(GLTF_idx).name, MsgBoxStyle.Exclamation, "GLTF Mesh Problem")
                Return False
            End If

            For j = 0 To 2

                '===============================================================================
                'position
                Dim cp_index As Integer = Math.Abs(mesh.GetPolygonVertex(i, j))
                If cp_index > max_cp_index Then max_cp_index = cp_index
                'Debug.WriteLine(vertexId.ToString + " " + cp_index.ToString)
                Dim vertex As GLTFVector4 = GLTFgrp(GLTF_idx).cPoints(cp_index)
                GLTFgrp(GLTF_idx).indicies(vertexId).v1 = vertexId
                '===============================================================================
                'normals
                Dim normal As New GLTFVector4
                If normal_layer.Mapping_Mode = GLTFLayerElement.MappingMode.ByPolygonVertex Then
                    Select Case normal_layer.Reference_Mode

                        Case GLTFLayerElement.ReferenceMode.Direct
                            normal = normal_layer.DirectArray.GetAt(vertexId)
                            Exit Select
                        Case GLTFLayerElement.ReferenceMode.IndexToDirect
                            Dim n_id = normal_layer.IndexArray.GetAt(vertexId)
                            normal = normal_layer.DirectArray.GetAt(n_id)

                    End Select

                ElseIf normal_layer.Mapping_Mode = GLTFLayerElement.MappingMode.ByControlPoint Then
                    Select Case normal_layer.Reference_Mode
                        Case GLTFLayerElement.ReferenceMode.Direct
                            normal = normal_layer.DirectArray.GetAt(cp_index)
                            Exit Select
                        Case GLTFLayerElement.ReferenceMode.IndexToDirect
                            Dim n_id = normal_layer.IndexArray.GetAt(vertexId)
                            normal = normal_layer.DirectArray.GetAt(n_id)
                    End Select

                End If
                '===============================================================================
                'UVs
                Dim uv As New GLTFVector2
                If uv_layer Is Nothing Then
                    MsgBox("No Uvs for mesh! ID:" + GLTFgrp(GLTF_idx).name, MsgBoxStyle.Exclamation, "GLTF Mesh Problem!")
                    Return False
                End If
                Select Case uv_layer.Mapping_Mode
                    Case GLTFLayerElement.MappingMode.ByControlPoint
                        Select Case uv_layer.Reference_Mode
                            Case GLTFLayerElement.ReferenceMode.Direct
                                uv = uv_layer.DirectArray.GetAt(cp_index)
                                Exit Select
                            Case GLTFLayerElement.ReferenceMode.IndexToDirect
                                Dim n_id = uv_layer.IndexArray.GetAt(cp_index)
                                uv = uv_layer.DirectArray.GetAt(n_id)

                        End Select
                        Exit Select
                    Case GLTFLayerElement.MappingMode.ByPolygonVertex
                        Dim uv_index = mesh.GetTextureUVIndex(i, j)
                        Select Case uv_layer.Reference_Mode
                            Case GLTFLayerElement.ReferenceMode.Direct
                            Case GLTFLayerElement.ReferenceMode.IndexToDirect
                                uv = uv_layer.DirectArray.GetAt(uv_index)
                        End Select
                End Select
                '===============================================================================
                'UVs
                Dim uv2 As New GLTFVector2
                If mesh.UVLayerCount = 2 Then

                    If uv2_Layer IsNot Nothing Then
                        GLTFgrp(GLTF_idx).has_uv2 = 1
                        save_has_uv2 = True
                        Select Case uv2_Layer.Mapping_Mode
                            Case GLTFLayerElement.MappingMode.ByControlPoint
                                Select Case uv2_Layer.Reference_Mode
                                    Case GLTFLayerElement.ReferenceMode.Direct
                                        uv2 = uv2_Layer.DirectArray.GetAt(cp_index)
                                        Exit Select
                                    Case GLTFLayerElement.ReferenceMode.IndexToDirect
                                        Dim n_id = uv2_Layer.IndexArray.GetAt(cp_index)
                                        uv2 = uv2_Layer.DirectArray.GetAt(n_id)

                                End Select
                                Exit Select
                            Case GLTFLayerElement.MappingMode.ByPolygonVertex
                                Dim uv2_index = mesh.GetTextureUVIndex(i, j)
                                Select Case uv2_Layer.Reference_Mode
                                    Case GLTFLayerElement.ReferenceMode.Direct
                                    Case GLTFLayerElement.ReferenceMode.IndexToDirect
                                        uv2 = uv2_Layer.DirectArray.GetAt(uv2_index)
                                End Select

                        End Select
                    End If
                End If
                '===============================================================================
                'vertex color
                Dim color1 As New GLTFColor
                If colorLayer1 IsNot Nothing Then
                    GLTFgrp(GLTF_idx).has_Vcolor = 0

                    Dim cv_refmode = colorLayer1.Reference_Mode
                    If cv_refmode = GLTFLayerElement.ReferenceMode.IndexToDirect Then
                        color1 = colorLayer1.DirectArray(colorLayer1.IndexArray.GetAt(vertexId))
                        GLTFgrp(GLTF_idx).has_Vcolor = 1

                    Else
                        If cv_refmode = GLTFLayerElement.ReferenceMode.Direct Then
                            color1 = colorLayer1.DirectArray(vertexId)
                            GLTFgrp(GLTF_idx).has_Vcolor = 1
                        End If
                    End If
                End If
                GLTFgrp(GLTF_idx).vertices(vertexId).x = vertex.X
                GLTFgrp(GLTF_idx).vertices(vertexId).y = vertex.Y
                GLTFgrp(GLTF_idx).vertices(vertexId).z = vertex.Z
                GLTFgrp(GLTF_idx).vertices(vertexId).u = uv.X
                GLTFgrp(GLTF_idx).vertices(vertexId).v = -uv.Y
                GLTFgrp(GLTF_idx).vertices(vertexId).u2 = uv2.X
                GLTFgrp(GLTF_idx).vertices(vertexId).v2 = -uv2.Y

                GLTFgrp(GLTF_idx).vertices(vertexId).nx = normal.X
                GLTFgrp(GLTF_idx).vertices(vertexId).ny = normal.Y
                GLTFgrp(GLTF_idx).vertices(vertexId).nz = normal.Z
                GLTFgrp(GLTF_idx).vertices(vertexId).n = packnormalGLTF888(normal)
                GLTFgrp(GLTF_idx).vertices(vertexId).index_1 = CByte(color1.Red * 255)
                GLTFgrp(GLTF_idx).vertices(vertexId).index_2 = CByte(color1.Green * 255)
                GLTFgrp(GLTF_idx).vertices(vertexId).index_3 = CByte(color1.Blue * 255)
                GLTFgrp(GLTF_idx).vertices(vertexId).index_4 = CByte(color1.Alpha * 255)

                GLTFgrp(GLTF_idx).vertices(vertexId).r = CByte(color1.Red * 255)
                GLTFgrp(GLTF_idx).vertices(vertexId).g = CByte(color1.Green * 255)
                GLTFgrp(GLTF_idx).vertices(vertexId).b = CByte(color1.Blue * 255)
                GLTFgrp(GLTF_idx).vertices(vertexId).a = CByte(color1.Alpha * 255)


                vertexId += 1
            Next
        Next
        ReDim Preserve GLTFgrp(GLTF_idx).vertices(vertexId - 1)
        ReDim Preserve GLTFgrp(GLTF_idx).indicies(vertexId - 1)
        GLTFgrp(GLTF_idx).nVertices_ = max_cp_index + 1

        'check_winding_order(GLTF_idx)

        create_TBNS2(GLTF_idx)

        Return True
    End Function

    Private Function fix_texture_path(s As String) As String
        If s.ToLower.Contains("vehicles") Then
            s = s.Replace("vehicles", "~")
            Dim a = s.Split("~")
            s = My.Settings.res_mods_path + "\vehicles" + a(1)
            Return s
        End If
        Return s
    End Function

    Private Sub process_GLTF_data()
        'we need to reorder the GLTF read by its ID tag
        Dim total = GLTFgrp.Length
        ReDim t_GLTF(total)
        Dim last As Integer = 1
        Dim pnt(30) As Integer
        'move to right locations....
        For i = 1 To GLTFgrp.Length - 1
            If GLTFgrp(i).name.ToLower.Contains("vehicle") Then

                Dim n = GLTFgrp(i).name
                Dim a = n.Split("~")
                Dim idx = Convert.ToInt32(a(2))
                move_GLTF_entry(t_GLTF(idx), GLTFgrp(i), i, idx)
                last += 1

            End If
        Next
        'move any new items to the end.
        For i = 1 To GLTFgrp.Length - 1
            If Not GLTFgrp(i).name.ToLower.Contains("vehicle") Then
                move_GLTF_entry(t_GLTF(last), GLTFgrp(i), last, i)
                last += 1
            End If
        Next
        ' write back the sorted GLTF entries.
        For i = 1 To GLTFgrp.Length - 1
            move_GLTF_entry(GLTFgrp(i), t_GLTF(i), last, i)
            Dim tn = GLTFgrp(i).name.Split("~")
            frmComponentView.add_to_GLTF_list(i, Path.GetFileNameWithoutExtension(tn(0)))
            frmReverseVertexWinding.add_to_GLTF_list(i, Path.GetFileNameWithoutExtension(tn(0)))
        Next

        ReDim t_GLTF(0) ' clean up some memory


        GC.Collect()

        get_component_index() 'build indexing table
    End Sub
    Private Sub move_GLTF_entry(ByRef GLTF_in As _grps, ByRef GLTF_out As _grps, ByVal i As Integer, ByVal idx As Integer)
        GLTF_in = New _grps

        GLTF_in.name = GLTF_out.name
        GLTF_in.color_name = GLTF_out.color_name
        GLTF_in.color_Id = GLTF_out.color_Id
        GLTF_in.normal_name = GLTF_out.normal_name
        GLTF_in.normal_Id = GLTF_out.normal_Id
        GLTF_in.call_list = GLTF_out.call_list
        GLTF_in.nPrimitives_ = GLTF_out.nPrimitives_
        GLTF_in.nVertices_ = GLTF_out.nVertices_
        GLTF_in.startIndex_ = GLTF_out.startIndex_
        GLTF_in.startVertex_ = GLTF_out.startVertex_
        GLTF_in.specular_name = GLTF_out.specular_name
        GLTF_in.specular_id = GLTF_out.specular_id
        GLTF_in.texture_count = GLTF_out.texture_count
        GLTF_in.has_uv2 = GLTF_out.has_uv2
        GLTF_in.has_Vcolor = GLTF_out.has_Vcolor
        GLTF_in.bumped = GLTF_out.bumped


        ReDim GLTF_in.matrix(15)
        For j = 0 To 15
            GLTF_in.matrix(j) = GLTF_out.matrix(j)
        Next
        ReDim GLTF_in.vertices(GLTF_out.vertices.Length - 1)
        For j = 0 To GLTF_out.vertices.Length - 1
            GLTF_in.vertices(j) = New vertice_
            GLTF_in.vertices(j).index_1 = GLTF_out.vertices(j).index_1
            GLTF_in.vertices(j).index_2 = GLTF_out.vertices(j).index_2
            GLTF_in.vertices(j).index_3 = GLTF_out.vertices(j).index_3
            GLTF_in.vertices(j).index_4 = GLTF_out.vertices(j).index_4

            GLTF_in.vertices(j).n = GLTF_out.vertices(j).n

            GLTF_in.vertices(j).x = GLTF_out.vertices(j).x
            GLTF_in.vertices(j).y = GLTF_out.vertices(j).y
            GLTF_in.vertices(j).z = GLTF_out.vertices(j).z

            GLTF_in.vertices(j).nx = GLTF_out.vertices(j).nx
            GLTF_in.vertices(j).ny = GLTF_out.vertices(j).ny
            GLTF_in.vertices(j).nz = GLTF_out.vertices(j).nz

            GLTF_in.vertices(j).n = GLTF_out.vertices(j).n
            GLTF_in.vertices(j).t = GLTF_out.vertices(j).t
            GLTF_in.vertices(j).bn = GLTF_out.vertices(j).bn

            GLTF_in.vertices(j).u = GLTF_out.vertices(j).u
            GLTF_in.vertices(j).v = GLTF_out.vertices(j).v

            GLTF_in.vertices(j).u2 = GLTF_out.vertices(j).u2
            GLTF_in.vertices(j).v2 = GLTF_out.vertices(j).v2

            GLTF_in.vertices(j).r = GLTF_out.vertices(j).r
            GLTF_in.vertices(j).g = GLTF_out.vertices(j).g
            GLTF_in.vertices(j).b = GLTF_out.vertices(j).b
            GLTF_in.vertices(j).a = GLTF_out.vertices(j).a

            GLTF_in.vertices(j).bnx = GLTF_out.vertices(j).bnx
            GLTF_in.vertices(j).bny = GLTF_out.vertices(j).bny
            GLTF_in.vertices(j).bnz = GLTF_out.vertices(j).bnz

            GLTF_in.vertices(j).tx = GLTF_out.vertices(j).tx
            GLTF_in.vertices(j).ty = GLTF_out.vertices(j).ty
            GLTF_in.vertices(j).tz = GLTF_out.vertices(j).tz

        Next
        ReDim GLTF_in.indicies(GLTF_out.indicies.Length - 1)
        For j = 0 To GLTF_out.indicies.Length - 1
            GLTF_in.indicies(j) = New uvect3
            GLTF_in.indicies(j).v1 = GLTF_out.indicies(j).v1
            'GLTF_in.indicies(j).v2 = GLTF_out.indicies(j).v2
            'GLTF_in.indicies(j).v3 = GLTF_out.indicies(j).v3
        Next

    End Sub

    Private Sub get_component_index()
        Dim ct, ht, tt, gt As Integer
        Dim c_cnt, h_cnt, t_cnt, g_cnt As Integer
        Dim odd_model As Boolean
        '---------------------------------------------------------------------------------------------------
        'find out if we have a wrongly named model in the GLTF
        odd_model = False
        For i = 1 To GLTFgrp.Length - 1
            If Not odd_model Then
                If GLTFgrp(i).name.ToLower.Contains("chassis") Or
                    GLTFgrp(i).name.ToLower.Contains("hull") Or
                    GLTFgrp(i).name.ToLower.Contains("turret") Or
                    GLTFgrp(i).name.ToLower.Contains("gun") Then
                Else
                    odd_model = True
                End If
            End If
        Next

        '---------------------------------------------------------------------------------------------------
        'sort out how many are of what type in the GLTF
        'we need to do this if parts have been added

        'now we create our index table
        Dim c As Integer = 1
        ReDim m_groups(4) ' there are 4 types... chassis, hull, turret and gun
        m_groups(1) = New mgrp_
        m_groups(2) = New mgrp_
        m_groups(3) = New mgrp_
        m_groups(4) = New mgrp_
        CRASH_MODE = False
        Dim ar() As String
        For i = 1 To GLTFgrp.Length - 1
            'set some booleans 
            GLTFgrp(i).is_carraige = False
            GLTFgrp(i).component_visible = True
            'figure out if this is a chasss component and get component counts
            If GLTFgrp(i).name.ToLower.Contains("chassis") Then
                If GLTFgrp(i).name.ToLower.Contains("\crash\") Then
                    CRASH_MODE = True
                End If
                GLTFgrp(i).is_carraige = True
                ReDim Preserve m_groups(1).list(ct)
                ReDim Preserve m_groups(1).group_list(ct)
                ReDim Preserve m_groups(1).f_name(ct)
                ReDim Preserve m_groups(1).section_names(ct)
                ReDim Preserve m_groups(1).package_id(ct)
                m_groups(1).list(ct) = i
                m_groups(1).cnt = ct + 1
                m_groups(1).m_type = 1
                ar = GLTFgrp(i).name.Split("~")
                m_groups(1).f_name(ct) = ar(0)
                If ar.Length > 1 Then
                    m_groups(1).package_id(ct) = CInt(ar(1))
                    m_groups(1).group_list(ct) = ar(2)
                    m_groups(1).section_names(ct) = ar(0)
                Else
                    m_groups(1).package_id(ct) = -1
                    m_groups(1).section_names(ct) = ar(0)
                End If
                ct += 1
            End If
            If GLTFgrp(i).name.ToLower.Contains("hull") Then
                ReDim Preserve m_groups(2).list(ht)
                ReDim Preserve m_groups(2).f_name(ht)
                ReDim Preserve m_groups(2).package_id(ht)
                m_groups(2).cnt = ht + 1
                m_groups(2).list(ht) = i
                m_groups(2).m_type = 2
                ar = GLTFgrp(i).name.Split("~")
                m_groups(2).f_name(ht) = ar(0)
                If ar.Length > 1 Then
                    m_groups(2).package_id(ht) = CInt(ar(1))
                Else
                    m_groups(2).package_id(ht) = -1
                End If
                ht += 1
            End If
            If GLTFgrp(i).name.ToLower.Contains("turret") Then
                ReDim Preserve m_groups(3).list(tt)
                ReDim Preserve m_groups(3).f_name(tt)
                ReDim Preserve m_groups(3).package_id(tt)
                m_groups(3).cnt = tt + 1
                m_groups(3).list(tt) = i
                m_groups(3).m_type = 3
                ar = GLTFgrp(i).name.Split("~")
                m_groups(3).f_name(tt) = ar(0)
                If ar.Length > 1 Then
                    m_groups(3).package_id(tt) = CInt(ar(1))
                Else
                    m_groups(3).package_id(tt) = -1
                End If
                tt += 1
            End If
            If GLTFgrp(i).name.ToLower.Contains("gun") Then
                ReDim Preserve m_groups(4).list(gt)
                ReDim Preserve m_groups(4).f_name(gt)
                ReDim Preserve m_groups(4).package_id(gt)
                m_groups(4).cnt = gt + 1
                m_groups(4).list(gt) = i
                m_groups(4).m_type = 4
                ar = GLTFgrp(i).name.Split("~")
                m_groups(4).f_name(gt) = ar(0)
                If ar.Length > 1 Then
                    m_groups(4).package_id(gt) = CInt(ar(1))
                Else
                    m_groups(4).package_id(gt) = -1
                End If
                gt += 1
            End If
        Next

        '---------------------------------------------------------------------------------------------------
        'we need to laod the tank.xml so if the user wants to write it out, its there.
        'now we will load the model from the package files
        For i = 1 To 4
            Dim kk As Integer = 0
            For j = 0 To m_groups(i).f_name.Length
                file_name = ""
                If m_groups(i).f_name(j).Contains("vehicles") Then
                    file_name = m_groups(i).f_name(j).Replace(".primitives_processed", ".model") 'assuming (0) has the correct name.
                    kk = j
                    Exit For
                End If
            Next
            If file_name = "" Then
                MsgBox("it looks like you change the name of one of the tank models. Don't do that!", MsgBoxStyle.Exclamation, "Opps!")
                MODEL_LOADED = False
                Return
            End If
            frmMain.info_Label.Text = "Loading Tank Component: " + file_name
            Application.DoEvents()
            file_name = file_name.Replace(".primitives", ".model")
            Dim ta = file_name.Split("\normal")
            current_tank_package = m_groups(i).package_id(kk)
            TANK_NAME = ta(0) + ":" + current_tank_package.ToString
            frmMain.Text = "File: " + ta(0)
            Dim success = build_primitive_data(True)
        Next
        '---------------------------------------------------------------------------------------------------
        'get the xml for this tank.
        file_name = file_name.Replace("\", "/")
        ar = file_name.Split("/")
        Dim xml_file = ar(0) + "\" + ar(1) + "\" + ar(2) + ".xml"
        frmMain.Text = "File: " + ar(0) + "\" + ar(1) + "\" + ar(2) ' title on main window

        frmMain.get_tank_parts_from_xml(xml_file, New DataSet)
        '---------------------------------------------------------------------------------------------------
        'sort out how many are of what type in the existing model
        For i = 1 To object_count
            If _group(i).name.ToLower.Contains("chassis") Then
                c_cnt += 1
                m_groups(1).existingCount = c_cnt
            End If
            If _group(i).name.ToLower.Contains("hull") Then
                h_cnt += 1
                m_groups(2).existingCount = h_cnt
            End If
            If _group(i).name.ToLower.Contains("turret") Then
                t_cnt += 1
                m_groups(3).existingCount = t_cnt
            End If
            If _group(i).name.ToLower.Contains("gun") Then
                g_cnt += 1
                m_groups(4).existingCount = g_cnt
            End If
        Next
        '---------------------------------------------------------------------------------------------------
        Dim t_GLTF, t_mdl As Integer
        t_GLTF = ct + ht + tt + gt
        t_mdl = c_cnt + h_cnt + t_cnt + g_cnt
        'if t_GLTF = t_mdl than we have the same componet counts.
        'Check of one of them has been modified.
        Dim flg, CB, HB, TB, GB As Boolean
        Dim c_new, h_new, t_new, g_new As Boolean
        CB = False : HB = False : TB = False : GB = False ' these default to false but set them anyway
        c_new = False : h_new = False : t_new = False : g_new = False
        If t_GLTF <> t_mdl Then
            If c_cnt <> ct Then 'something added?
                CB = True
                c_new = True
            End If
            If h_cnt <> ht Then 'something added?
                HB = True
                h_new = True
            End If
            If t_cnt <> tt Then 'something added?
                TB = True
                t_new = True
            End If
            If g_cnt <> gt Then 'something added?
                GB = True
                g_new = True
            End If
        Else
            For i = 1 To object_count
                flg = False
                If _group(i).nVertices_ <> GLTFgrp(i).nVertices_ Then 'polygons removed or added?
                    flg = True : GoTo whichone
                End If
                Try
                    For j As UInt32 = 0 To _group(i).indicies.Length - 2
                        Dim p1 = _group(i).indicies(j + 1).v1 - _group(i).startVertex_
                        Dim p2 = _group(i).indicies(j + 1).v2 - _group(i).startVertex_
                        Dim p3 = _group(i).indicies(j + 1).v3 - _group(i).startVertex_
                        Dim vg_1 = _group(i).vertices(p1)
                        Dim vg_2 = _group(i).vertices(p2)
                        Dim vg_3 = _group(i).vertices(p3)
                        Dim f1 = GLTFgrp(i).indicies((j * 3) + 0).v1
                        Dim f2 = GLTFgrp(i).indicies((j * 3) + 1).v1
                        Dim f3 = GLTFgrp(i).indicies((j * 3) + 2).v1
                        Dim vf_1 = GLTFgrp(i).vertices(f1)
                        Dim vf_2 = GLTFgrp(i).vertices(f2)
                        Dim vf_3 = GLTFgrp(i).vertices(f3)
                        '

                        'check every verts x,y and z for non match
                        'p1 -----------------------------------------
                        If vg_1.x <> vf_1.x Then
                            flg = True
                        End If
                        If vg_1.y <> vf_1.y Then
                            flg = True
                        End If
                        If vg_1.z <> vf_1.z Then
                            flg = True
                        End If
                        'p2 -----------------------------------------
                        If vg_2.x <> vf_2.x Then
                            flg = True
                        End If
                        If vg_2.y <> vf_2.y Then
                            flg = True
                        End If
                        If vg_2.z <> vf_2.z Then
                            flg = True
                        End If
                        'p3 -----------------------------------------
                        If vg_3.x <> vf_3.x Then
                            flg = True
                        End If
                        If vg_3.y <> vf_3.y Then
                            flg = True
                        End If
                        If vg_3.z <> vf_3.z Then
                            flg = True
                        End If
                    Next
                Catch ex As Exception

                End Try

whichone:
                If flg Then ' if true than either the count is different or the vertices are changed
                    If _group(i).name.ToLower.Contains("chassis") Then
                        'check if the treads have been changed. The can NOT 
                        If _group(i).color_name.ToLower.Contains("tracks") And CB Then
                            'MsgBox("It appears you have removed or added" + vbCrLf + _
                            '       " vertices to the rubber band tracks!" + vbCrLf + _
                            '       "You can ignore this warning!!", _
                            '       MsgBoxStyle.Exclamation, "Oh My..")
                        Else
                            CB = True

                        End If
                    End If
                    If _group(i).name.ToLower.Contains("hull") Then
                        HB = True
                    End If
                    If _group(i).name.ToLower.Contains("turret") Then
                        TB = True
                    End If
                    If _group(i).name.ToLower.Contains("gun") Then
                        GB = True
                    End If
                End If
            Next

        End If
        For i = 1 To GLTFgrp.Length - 1
            If Not GLTFgrp(i).name.Contains("vehicles\") Then
                GLTFgrp(i).is_new_model = True
                GLTFgrp(i).is_GAmap = 0 ' not PBS
                GLTFgrp(i).alphaTest = 1
            Else
                GLTFgrp(i).is_GAmap = 1 'is PBS
                GLTFgrp(i).alphaTest = _group(i).alphaTest
            End If
        Next
        'need to find out if there is a dangling model that was imported.
        'one that was not assigned via name to a group
        If odd_model Then
            MsgBox("It appears you have added a model that is not assigned to a group." + vbCrLf +
                    "Make sure you renamed the model you created to include a group name.." + vbCrLf +
                    "The name should include one of these : Chassis, Hull, Turret or Gun." + vbCrLf +
                    "I CAN NOT add a new group to a tank model. I can Only add new items to a group." + vbCrLf +
                    "You will not beable to save this model!", MsgBoxStyle.Exclamation, "Import Issue")
            frmMain.m_write_primitive.Enabled = False
        Else
            frmMain.m_write_primitive.Enabled = True
        End If
        'We give the user the opertunity to extract the model. We need some where to write any changed data too.
        file_name = file_name.Replace("/", "\")
        ar = file_name.Split("\")
        Dim fn = ar(0) + "\" + ar(1) + "\" + ar(2)
        current_tank_name = fn ' Path.GetDirectoryName(file_name)
        Dim dp = My.Settings.res_mods_path + "\" + fn
        frmWritePrimitive.SAVE_NAME = dp
        If Not Directory.Exists(dp) Then
            If MsgBox("It appears You have not extracted data for this model." + vbCrLf +
                      "There is no place to save this new Model." + vbCrLf +
                       "Would you like to extract the data from the .PKG files?", MsgBoxStyle.YesNo, "Extract?") = MsgBoxResult.Yes Then
                file_name = "1:dummy:" + Path.GetFileNameWithoutExtension(dp.Replace("/", "\"))
                frmMain.m_extract.PerformClick()
            End If

        End If
        'set which group has new models or changed data
        frmWritePrimitive.Visible = True
        If CRASH_MODE Then
            frmWritePrimitive.m_write_crashed.Checked = True
        Else
            frmWritePrimitive.m_write_crashed.Checked = False
        End If

        frmWritePrimitive.cew_cb.Checked = CB
        'frmWritePrimitive.cew_cb.Enabled = False
        'm_groups(1).changed = False ' = CB
        m_groups(1).changed = CB
        m_groups(1).new_objects = c_new

        frmWritePrimitive.hew_cb.Checked = HB
        m_groups(2).changed = HB
        m_groups(2).new_objects = h_new

        frmWritePrimitive.tew_cb.Checked = TB
        m_groups(3).changed = TB
        m_groups(3).new_objects = t_new

        frmWritePrimitive.gew_cb.Checked = GB
        'frmWritePrimitive.gew_cb.Enabled = False
        m_groups(4).changed = GB
        m_groups(4).new_objects = g_new


        '####################################
        'All the tank parts are loaded so
        'lets create the color picking lists.
        'This should speed up color picking a lot.
        Dim r, b, g, a As Byte
        For i = 1 To GLTFgrp.Length - 1
            GLTFgrp(i).visible = True
            Dim cpl = Gl.glGenLists(1)
            GLTFgrp(i).vertex_pick_list = cpl
            Gl.glNewList(cpl, Gl.GL_COMPILE)
            a = i + 10
            Gl.glBegin(Gl.GL_TRIANGLES)
            For k As UInt32 = 0 To GLTFgrp(i).nPrimitives_ * 3 - 1 Step 3

                Dim p1 = GLTFgrp(i).indicies(k + 0).v1
                Dim p2 = GLTFgrp(i).indicies(k + 1).v1
                Dim p3 = GLTFgrp(i).indicies(k + 2).v1
                Dim v1 = GLTFgrp(i).vertices(p1)
                Dim v2 = GLTFgrp(i).vertices(p2)
                Dim v3 = GLTFgrp(i).vertices(p3)
                Dim t = CInt((k / 3) + 1)
                r = t And &HFF
                g = (t And &HFF00) >> 8
                b = (t And &HFF0000) >> 16
                Gl.glColor4ub(r, g, b, a)
                Gl.glVertex3f(v1.x, v1.y, v1.z)
                Gl.glVertex3f(v2.x, v2.y, v2.z)
                Gl.glVertex3f(v3.x, v3.y, v3.z)
            Next
            Gl.glEnd()
            Gl.glEndList()
        Next
        'create pick lists
        For i = 1 To object_count
            Dim cpl = Gl.glGenLists(1)
            _object(i).vertex_pick_list = cpl
            Gl.glNewList(cpl, Gl.GL_COMPILE)
            a = i + 10
            If _object(i).visible Then
                Gl.glBegin(Gl.GL_TRIANGLES)
                For k As UInt32 = 1 To _object(i).count
                    Dim v1 = _object(i).tris(k).v1
                    Dim v2 = _object(i).tris(k).v2
                    Dim v3 = _object(i).tris(k).v3
                    r = k And &HFF
                    g = (k And &HFF00) >> 8
                    b = (k And &HFF0000) >> 16
                    Gl.glColor4ub(r, g, b, a)
                    Gl.glVertex3f(v1.x, v1.y, v1.z)
                    Gl.glVertex3f(v2.x, v2.y, v2.z)
                    Gl.glVertex3f(v3.x, v3.y, v3.z)
                Next
                Gl.glEnd()
            End If
            Gl.glEndList()
        Next
        frmMain.chassis_cb.Checked = True
        frmMain.hull_cb.Checked = True
        frmMain.turret_cb.Checked = True
        frmMain.gun_cb.Checked = True
        frmMain.m_view_res_mods_folder.Enabled = True
        frmWritePrimitive.Visible = False
        frmMain.find_icon_image(TANK_NAME)
        Application.DoEvents()
        MODEL_LOADED = True
        frmMain.m_hide_show_components.Enabled = True
        frmMain.m_set_vertex_winding_order.Enabled = True
    End Sub

    Private Sub check_winding_order(ByVal i As Integer)


        For k As UInt32 = 0 To GLTFgrp(i).nPrimitives_ * 3 - 1 Step 3

            Dim p1 = GLTFgrp(i).indicies(k + 0).v1
            Dim p2 = GLTFgrp(i).indicies(k + 1).v1
            Dim p3 = GLTFgrp(i).indicies(k + 2).v1
            Dim v1, v2, v3 As SharpDX.Vector3
            v1.X = GLTFgrp(i).vertices(p1).x
            v1.Y = GLTFgrp(i).vertices(p1).y
            v1.Z = GLTFgrp(i).vertices(p1).z

            v2.X = GLTFgrp(i).vertices(p2).x
            v2.Y = GLTFgrp(i).vertices(p2).y
            v2.Z = GLTFgrp(i).vertices(p2).z

            v3.X = GLTFgrp(i).vertices(p3).x
            v3.Y = GLTFgrp(i).vertices(p3).y
            v3.Z = GLTFgrp(i).vertices(p3).z

            Dim Dir = SharpDX.Vector3.Cross(v2 - v1, v3 - v1)
            Dim n = SharpDX.Vector3.Normalize(Dir)
            Dim n2 As New GLTFVector4
            n2.X = n.X
            n2.Y = n.Y
            n2.Z = n.Z


            GLTFgrp(i).vertices(p1).nx = n.X
            GLTFgrp(i).vertices(p1).ny = n.Y
            GLTFgrp(i).vertices(p1).nz = n.Z
            GLTFgrp(i).vertices(p1).n = packnormalGLTF888(n2)

            GLTFgrp(i).vertices(p2).nx = n.X
            GLTFgrp(i).vertices(p2).ny = n.Y
            GLTFgrp(i).vertices(p2).nz = n.Z
            GLTFgrp(i).vertices(p2).n = GLTFgrp(i).vertices(p1).n

            GLTFgrp(i).vertices(p3).nx = n.X
            GLTFgrp(i).vertices(p3).ny = n.Y
            GLTFgrp(i).vertices(p3).nz = n.Z
            GLTFgrp(i).vertices(p3).n = GLTFgrp(i).vertices(p1).n


        Next

    End Sub
    Public Sub make_GLTF_display_lists(ByVal cnt As Integer, ByVal jj As Integer)
        Gl.glBegin(Gl.GL_TRIANGLES)
        For z As UInt32 = 0 To (cnt) - 1
            make_triangle(jj, GLTFgrp(jj).indicies(z).v1)
        Next
        Gl.glEnd()
    End Sub

    Private Sub make_triangle(ByVal jj As Integer, ByVal i As Integer)
        Gl.glNormal3f(GLTFgrp(jj).vertices(i).nx, GLTFgrp(jj).vertices(i).ny, GLTFgrp(jj).vertices(i).nz)
        Gl.glMultiTexCoord2f(Gl.GL_TEXTURE0, -GLTFgrp(jj).vertices(i).u, GLTFgrp(jj).vertices(i).v)
        Gl.glMultiTexCoord3f(Gl.GL_TEXTURE1, GLTFgrp(jj).vertices(i).tx, GLTFgrp(jj).vertices(i).ty, GLTFgrp(jj).vertices(i).tz)
        Gl.glMultiTexCoord3f(Gl.GL_TEXTURE2, GLTFgrp(jj).vertices(i).bnx, GLTFgrp(jj).vertices(i).bny, GLTFgrp(jj).vertices(i).bnz)
        If GLTFgrp(jj).has_Vcolor Then
            Gl.glMultiTexCoord3f(Gl.GL_TEXTURE3, CSng(GLTFgrp(jj).vertices(i).index_1 / 255.0!),
                                  CSng(GLTFgrp(jj).vertices(i).index_2 / 255.0!),
                                  CSng(GLTFgrp(jj).vertices(i).index_3 / 255.0!))
        Else
            Gl.glMultiTexCoord3f(Gl.GL_TEXTURE3, 0.0!, 0.0!, 0.0!)
        End If

        Gl.glTexCoord2f(-GLTFgrp(jj).vertices(i).u, GLTFgrp(jj).vertices(i).v)

        Gl.glVertex3f(GLTFgrp(jj).vertices(i).x, GLTFgrp(jj).vertices(i).y, GLTFgrp(jj).vertices(i).z)

    End Sub

    Private Sub setGLTFMatrix(ByRef m_() As Double, ByRef fb As Matrix4)
        Dim m As New Matrix4(m_)

        Dim q As Quaternion
        Dim t, s As Vector3
        m.Decompose(t, s, q)


        fb = q.ToMatrix

    End Sub


#Region "TBN Creation functions"

    Public Sub create_TBNS(ByVal id As UInt32)
        Dim cnt = GLTFgrp(id).nPrimitives_
        Dim p1, p2, p3 As UInt32
        For i As UInt32 = 0 To cnt - 1
            p1 = GLTFgrp(id).indicies(i).v1
            p2 = GLTFgrp(id).indicies(i).v2
            p3 = GLTFgrp(id).indicies(i).v3
            Dim tan, bn As vect3
            Dim v1, v2, v3 As vect3
            Dim u1, u2, u3 As vect3
            v1.x = -GLTFgrp(id).vertices(p1).x
            v1.y = GLTFgrp(id).vertices(p1).y
            v1.z = GLTFgrp(id).vertices(p1).z

            v2.x = -GLTFgrp(id).vertices(p2).x
            v2.y = GLTFgrp(id).vertices(p2).y
            v2.z = GLTFgrp(id).vertices(p2).z

            v3.x = -GLTFgrp(id).vertices(p3).x
            v3.y = GLTFgrp(id).vertices(p3).y
            v3.z = GLTFgrp(id).vertices(p3).z
            '
            u1.x = GLTFgrp(id).vertices(p1).u
            u1.y = GLTFgrp(id).vertices(p1).v

            u2.x = GLTFgrp(id).vertices(p2).u
            u2.y = GLTFgrp(id).vertices(p2).v

            u3.x = GLTFgrp(id).vertices(p3).u
            u3.y = GLTFgrp(id).vertices(p3).v
            ComputeTangentBasis(v1, v2, v3, u1, u2, u3, tan, bn) ' calculate tan and biTan

            save_tbn(id, tan, bn, p1) ' puts xyz values in vertex
            save_tbn(id, tan, bn, p2)
            save_tbn(id, tan, bn, p3)

            GLTFgrp(id).vertices(p1).t = packnormalGLTF888(toGLTFv(tan)) 'packs and puts the uint value in to the vertex
            GLTFgrp(id).vertices(p1).bn = packnormalGLTF888(toGLTFv(bn))
            GLTFgrp(id).vertices(p2).t = packnormalGLTF888(toGLTFv(tan))
            GLTFgrp(id).vertices(p2).bn = packnormalGLTF888(toGLTFv(bn))
            GLTFgrp(id).vertices(p3).t = packnormalGLTF888(toGLTFv(tan))
            GLTFgrp(id).vertices(p3).bn = packnormalGLTF888(toGLTFv(bn))
        Next
        Return
    End Sub
    Public Sub create_TBNS2(ByVal id As UInt32)
        Dim cnt = GLTFgrp(id).nPrimitives_ * 3
        Dim p1, p2, p3 As UInt32
        For i As UInt32 = 0 To cnt - 1 Step 3
            p1 = GLTFgrp(id).indicies(i).v1
            p2 = GLTFgrp(id).indicies(i + 1).v1
            p3 = GLTFgrp(id).indicies(i + 2).v1
            Dim tan, bn As vect3
            Dim v1, v2, v3 As vect3
            Dim u1, u2, u3 As vect3
            v1.x = -GLTFgrp(id).vertices(p1).x
            v1.y = GLTFgrp(id).vertices(p1).y
            v1.z = GLTFgrp(id).vertices(p1).z

            v2.x = -GLTFgrp(id).vertices(p2).x
            v2.y = GLTFgrp(id).vertices(p2).y
            v2.z = GLTFgrp(id).vertices(p2).z

            v3.x = -GLTFgrp(id).vertices(p3).x
            v3.y = GLTFgrp(id).vertices(p3).y
            v3.z = GLTFgrp(id).vertices(p3).z
            '
            u1.x = GLTFgrp(id).vertices(p1).u
            u1.y = GLTFgrp(id).vertices(p1).v

            u2.x = GLTFgrp(id).vertices(p2).u
            u2.y = GLTFgrp(id).vertices(p2).v

            u3.x = GLTFgrp(id).vertices(p3).u
            u3.y = GLTFgrp(id).vertices(p3).v
            ComputeTangentBasis(v1, v2, v3, u1, u2, u3, tan, bn) ' calculate tan and biTan

            save_tbn(id, tan, bn, p1) ' puts xyz values in vertex
            save_tbn(id, tan, bn, p2)
            save_tbn(id, tan, bn, p3)

            GLTFgrp(id).vertices(p1).t = packnormalGLTF888(toGLTFv(tan)) 'packs and puts the uint value in to the vertex
            GLTFgrp(id).vertices(p1).bn = packnormalGLTF888(toGLTFv(bn))
            GLTFgrp(id).vertices(p2).t = packnormalGLTF888(toGLTFv(tan))
            GLTFgrp(id).vertices(p2).bn = packnormalGLTF888(toGLTFv(bn))
            GLTFgrp(id).vertices(p3).t = packnormalGLTF888(toGLTFv(tan))
            GLTFgrp(id).vertices(p3).bn = packnormalGLTF888(toGLTFv(bn))
        Next
        Return
    End Sub

    Private Sub save_tbn(id As Integer, tan As vect3, bn As vect3, i As Integer)
        GLTFgrp(id).vertices(i).tx = tan.x
        GLTFgrp(id).vertices(i).ty = tan.y
        GLTFgrp(id).vertices(i).tz = tan.z
        GLTFgrp(id).vertices(i).bnx = bn.x
        GLTFgrp(id).vertices(i).bny = bn.y
        GLTFgrp(id).vertices(i).bnz = bn.z

    End Sub
    Public Function toGLTFv(ByVal inv As vect3) As GLTFVector4
        Dim v As New GLTFVector4
        v.X = inv.x
        v.Y = inv.y
        v.Z = inv.z
        Return v
    End Function


    Public Function normalize(ByVal normal As vect3) As vect3
        Dim len As Single = Sqrt((normal.x * normal.x) + (normal.y * normal.y) + (normal.z * normal.z))

        ' avoid division by 0
        If len = 0.0F Then len = 1.0F
        Dim v As vect3
        ' reduce to unit size
        v.x = (normal.x / len)
        v.y = (normal.y / len)
        v.z = (normal.z / len)

        Return v
    End Function
    Public Function mulvect3(ByVal v1 As vect3, ByVal v As Single) As vect3
        v1.x *= v
        v1.y *= v
        v1.z *= v
        Return v1
    End Function
    Public Function addvect3(ByVal v1 As vect3, ByVal v2 As vect3) As vect3
        v1.x += v2.x
        v1.y += v2.y
        v1.z += v2.z
        Return v1
    End Function
    Public Function subvect3(ByVal v1 As vect3, ByVal v2 As vect3) As vect3
        v1.x -= v2.x
        v1.y -= v2.y
        v1.z -= v2.z
        Return v1
    End Function
    Public Function subvect2(ByVal v1 As vect3, ByVal v2 As vect3) As vect3
        v1.x -= v2.x
        v1.y -= v2.y
        Return v1
    End Function
#End Region

#End Region

#Region "Export Helpers"

    Private Sub build_GLTF_matrix(ByVal idx As Integer, ByVal fm As Matrix4)
        ReDim GLTFgrp(idx).matrix(15)
        For i = 0 To 15
            GLTFgrp(idx).matrix(i) = CSng(fm((i >> 2 And &H3), (i And &H3)))
        Next

    End Sub

    Private Function s_to_int(ByRef n As Single) As Int32
        Dim i As Int32
        i = lookup(((n + 1.0) * 0.5) * 254)
        Return i
    End Function

    Public Function packnormalGLTF_old(ByVal n As Vector4) As UInt32
        'ctz is my special C++ function to pack the vector into a Uint32
        ctz.init_x(n.x)
        ctz.init_y(n.y)
        ctz.init_z(n.z)
        Return ctz.pack(1)
    End Function

    Public Function GLTF_create_material(id As Integer) As PbrMaterial
        Dim m_name As String = "Material"
        Dim s_name As String = "Phong"

        Dim co As Vector3
        co.x = 0.6
        co.y = 0.6
        co.z = 0.6
        Dim lMaterial As New PbrMaterial(co)
        'Need a name for this material
        lMaterial.Name = ("material" + id.ToString)
        Return lMaterial
    End Function
    Public Function GLTF_create_material_blender(id As Integer, ByVal n As String) As PbrMaterial
        Dim m_name As String = "Material"
        Dim s_name As String = "Phong"

        Dim co As Vector3
        co.x = 0.6
        co.y = 0.6
        co.z = 0.6
        Dim lMaterial As New PbrMaterial(co)
        'Need a name for this material
        lMaterial.Name = ("material" + id.ToString)

        Return lMaterial
    End Function

    Public Function GLTF_create_texture_primtive(id As Integer) As Texture
        'need a name for this texture
        Dim text As New Texture("Texture_" + id.ToString)
        Dim t_name As String = "Texture_" + id.ToString
        ' Set text properties.
        Try

            If _group(id).is_atlas_type Then
                Dim idx = _group(id).g_atlas_indexs.x
                t_name = GLTF_Texture_path + "\" + "Atlas_AM_map_" + id.ToString + ".png"
            Else
                t_name = GLTF_Texture_path + "\" +
                                            Path.GetFileNameWithoutExtension(_group(id).color_name) + ".png"
            End If
        Catch ex As Exception

        End Try
        text.Name = t_name
        text.FileName = t_name 'Get the text path from the list
        text.MagFilter = TextureFilter.Linear
        text.MinFilter = TextureFilter.Linear
        text.MipFilter = TextureFilter.Anisotropic
        text.EnableMipMap = True
        Return text
    End Function
    Public Function GLTF_create_texture(id As Integer) As Texture
        'need a name for this texture
        Dim text As New Texture(GLTF_Texture_path + "\" + Path.GetFileNameWithoutExtension(textures(id).c_name) + ".png")
        ' Set texture properties.
        text.Name = GLTF_Texture_path + "\" + Path.GetFileNameWithoutExtension(textures(id).c_name) + ".png"
        text.FileName = GLTF_Texture_path + "\" + Path.GetFileNameWithoutExtension(textures(id).c_name) + ".png" 'Get the Texture path from the list

        text.MagFilter = TextureFilter.Linear
        text.MinFilter = TextureFilter.Linear
        text.MipFilter = TextureFilter.Anisotropic
        text.EnableMipMap = True
        Return text
    End Function

    Public Function GLTF_create_texture_N_primitive(id As Integer) As Texture
        'need a name for this texture

        Dim text As New Texture("nTexture_" + id.ToString)
        Dim t_name As String = ""
        Try

            If _group(id).is_atlas_type Then
                Dim idx = _group(id).g_atlas_indexs.x
                t_name = GLTF_Texture_path + "\" + "Atlas_NM_map_" + id.ToString + ".png"
            Else
                t_name = GLTF_Texture_path + "\" +
                                            Path.GetFileNameWithoutExtension(_group(id).normal_name) + ".png"
            End If
        Catch ex As Exception

        End Try
        ' Set texture properties.
        text.Name = t_name
        text.FileName = t_name
        text.MagFilter = TextureFilter.Linear
        text.MinFilter = TextureFilter.Linear
        text.MipFilter = TextureFilter.Anisotropic
        text.EnableMipMap = True
        Return text
    End Function
    Public Function GLTF_create_texture_N(id As Integer) As Texture
        'need a name for this texture
        'Dim texture = GLTFTexture.Create(pManager, "NormalMap" + ":" + id.ToString("000"))
        Dim text As New Texture(GLTF_Texture_path + "\" + Path.GetFileNameWithoutExtension(textures(id).n_name) + ".png")
        ' Set texture properties.
        text.Name = GLTF_Texture_path + "\" + Path.GetFileNameWithoutExtension(textures(id).n_name) + ".png"
        text.FileName = GLTF_Texture_path + "\" + Path.GetFileNameWithoutExtension(textures(id).n_name) + ".png" 'Get the Texture path from the list
        text.MagFilter = TextureFilter.Linear
        text.MinFilter = TextureFilter.Linear
        text.MipFilter = TextureFilter.Anisotropic
        text.EnableMipMap = True
        Return text
    End Function

    Private Function load_matrix_decompose(data() As Double, ByRef trans As SharpDX.Vector3, ByRef scale As SharpDX.Vector3, ByRef rot As SharpDX.Quaternion) As SharpDX.Matrix
        Dim m_ As New SharpDX.Matrix
        For i = 0 To 3
            For k = 0 To 3
                m_(i, k) = data((i * 4) + k)
            Next
        Next
        'm_(0, 0) *= -1.0
        'm_(2, 0) *= -1.0
        'm_(2, 0) *= -1.0
        'm_(2, 2) *= -1.0
        m_.Decompose(scale, rot, trans)
        round_error(rot.X)
        round_error(rot.Y)
        round_error(rot.Z)
        round_error(rot.W)
        Return m_
    End Function

    Private Sub round_error(ByRef val As Single)
        val = Round(val, 6, MidpointRounding.AwayFromZero)
    End Sub

    Public Function GLTF_create_mesh(model_name As String, id As Integer, pManager As GLTFSdkManager) As Mesh
        Dim myMesh As New Mesh(model_name)
        Dim cnt = _group(id).nPrimitives_
        Dim off As UInt32
        Dim v As vect3Norm
        Dim v4 As New Vector4
        Dim I As Integer
        off = _group(id).startVertex_

        '--------------------------------------------------------------------------
        '--------------------------------------------------------------------------
        'first we load all the vertices for the _group data
        'add in the vertices (or control points as its called in GLTF)
        Dim cp_array(_group(id).vertices.Length) As Vector4
        For I = 0 To _group(id).vertices.Length - 1
            cp_array(I) = New Vector4()
            cp_array(I).x = _group(id).vertices(I).x
            cp_array(I).y = _group(id).vertices(I).y
            cp_array(I).z = _group(id).vertices(I).z
        Next

        myMesh.ControlPoints.AddRange(cp_array)  ' push it in to the mesh object
        'create or get the layer 0

        Dim indi_list(_group(id).indicies.Length) As Vector4
        For I = 0 To _group(id).indicies.Length - 1
            indi_list(I).x = _group(id).indicies(I).v1
            indi_list(I).y = _group(id).indicies(I).v2
            indi_list(I).z = _group(id).indicies(I).v3
        Next
        '--------------------------------------------------------------------------
        Dim nElement As New VertexElementNormal()
        nElement.SetData(indi_list)

        Dim uvElement As New VertexElementUV()
        Dim uv_list(_group(id).vertices.Length) As Vector4

        For I = 0 To _group(id).nPrimitives_
            uv_list(I).x = _group(id).vertices(I).u
            uv_list(I).y = _group(id).vertices(I).v

        Next
        uvElement.SetData(uv_list)

        Dim n_list(_group(id).vertices.Length) As Vector4
        For I = 0 To _group(id).vertices.Length * 3 - 1 Step 3
            Dim v1 = _group(id).indicies(I).v1
            Dim v2 = _group(id).indicies(I).v2
            Dim v3 = _group(id).indicies(I).v3
            v = unpackNormal(_group(id).vertices(v1 - off).n, _group(id).BPVT_mode)
            v4.x = v.nx
            v4.y = v.ny
            v4.z = v.nz
            n_list(I) = v4
            v = unpackNormal(_group(id).vertices(v2 - off).n, _group(id).BPVT_mode)
            v4.x = v.nx
            v4.y = v.ny
            v4.z = v.nz
            n_list(I + 1) = v4
            v = unpackNormal(_group(id).vertices(v3 - off).n, _group(id).BPVT_mode)
            v4.x = v.nx
            v4.y = v.ny
            v4.z = v.nz
            n_list(I + 2) = v4

        Next
        nElement.SetData(n_list)

        myMesh.AddElement(nElement)
        myMesh.AddElement(uvElement)
        '--------------------------------------------------------------------------
        '--------------------------------------------------------------------------
        'weights .. no idea how to export them from the vertex data :(
        '--------------------------------------------------------------------------
        'export vertex colors
        If _group(id).header.Contains("iii") Then ' has indices
            Dim colorLayer1 As New VertexElementVertexColor()
            colorLayer1.Name = "VertexColor"
            Dim color As New Vector4
            For I = 1 To _group(id).nPrimitives_
                Dim indi = _group(id).indicies(I)
                color.x = CDbl(_group(id).vertices(indi.v1 - off).index_1 / 255)
                color.y = CDbl(_group(id).vertices(indi.v1 - off).index_2 / 255)
                color.z = CDbl(_group(id).vertices(indi.v1 - off).index_3 / 255)
                color.w = 1.0 'CDbl(_group(id).vertices(I).index_4 / 255)
                colorLayer1.Add(color)

                color.x = CDbl(_group(id).vertices(indi.v2 - off).index_1 / 255)
                color.y = CDbl(_group(id).vertices(indi.v2 - off).index_2 / 255)
                color.z = CDbl(_group(id).vertices(indi.v2 - off).index_3 / 255)
                color.w = 1.0 'CDbl(_group(id).vertices(I).index_4 / 255)
                colorLayer1.DirectArray.Add(color)

                color.x = CDbl(_group(id).vertices(indi.v3 - off).index_1 / 255)
                color.y = CDbl(_group(id).vertices(indi.v3 - off).index_2 / 255)
                color.z = CDbl(_group(id).vertices(indi.v3 - off).index_3 / 255)
                color.w = 1.0 'CDbl(_group(id).vertices(I).index_4 / 255)
                colorLayer1.DirectArray.Add(color)
            Next
            layer.VertexColors = colorLayer1
        End If

        '--------------------------------------------------------------------------
        '--------------------------------------------------------------------------
        Dim v_2 As New GLTFVector2
        Dim UV2Layer As GLTFLayerElementUV = Nothing
        If _group(id).has_uv2 = 1 Then

            UV2Layer = GLTFLayerElementUV.Create(myMesh, "UV2")
            UV2Layer.Mapping_Mode = GLTFLayerElement.MappingMode.ByControlPoint
            UV2Layer.Reference_Mode = GLTFLayerElement.ReferenceMode.Direct
            layer.SetUVs(UV2Layer, GLTFLayerElement.LayerElementType.AmbientTextures)
            For I = 0 To myMesh.ControlPointsCount - 1
                If frmGLTF.flip_u.Checked Then
                    v_2.X = _group(id).vertices(I).u2 * -1
                Else
                    v_2.X = _group(id).vertices(I).u2
                End If

                If frmGLTF.flip_v.Checked Then
                    v_2.Y = _group(id).vertices(I).v2 * -1
                Else
                    v_2.Y = _group(id).vertices(I).v2
                End If
                UV2Layer.DirectArray.Add(v_2)

            Next
            UV2Layer.IndexArray.Count = _group(id).nPrimitives_
        End If
        '--------------------------------------------------------------------------
        '--------------------------------------------------------------------------
        ' Create UV for Diffuse channel
        Dim UVDiffuseLayer As GLTFLayerElementUV = GLTFLayerElementUV.Create(myMesh, "DiffuseUV")
        UVDiffuseLayer.Mapping_Mode = GLTFLayerElement.MappingMode.ByControlPoint
        UVDiffuseLayer.Reference_Mode = GLTFLayerElement.ReferenceMode.Direct
        layer.SetUVs(UVDiffuseLayer, GLTFLayerElement.LayerElementType.DiffuseTextures)
        For I = 0 To myMesh.ControlPointsCount - 1
            If frmGLTF.flip_u.Checked Then
                v_2.X = _group(id).vertices(I).u * -1
            Else
                v_2.X = _group(id).vertices(I).u
            End If

            If Not frmGLTF.flip_v.Checked Then
                v_2.Y = _group(id).vertices(I).v * -1
            Else
                v_2.Y = _group(id).vertices(I).v
            End If
            UVDiffuseLayer.DirectArray.Add(v_2)
        Next

        '--------------------------------------------------------------------------
        '--------------------------------------------------------------------------

        'Now we have set the UVs as eINDEX_TO_DIRECT reference and in eBY_POLYGON_VERTEX  mapping mode
        'we must update the size of the index array.
        UVDiffuseLayer.IndexArray.Count = _group(id).nPrimitives_
        'in the same way with Textures, but we are in eBY_POLYGON,
        'we should have N polygons (1 for each faces of the object)
        Dim pos As UInt32 = 0
        Dim n As UInt32 = 1
        Dim j As UInt32 = 0
        For I = 0 To _group(id).nPrimitives_ - 1
            myMesh.BeginPolygon(-1, -1, -1, False)

            j = 0
            pos = _group(id).indicies(n).v1 - off
            myMesh.AddPolygon(pos)
            UVDiffuseLayer.IndexArray.SetAt(pos, j)
            If _group(id).has_uv2 = 1 Then
                UV2Layer.IndexArray.SetAt(pos, j)
            End If
            j += 1
            pos = _group(id).indicies(n).v2 - off
            myMesh.AddPolygon(pos)
            UVDiffuseLayer.IndexArray.SetAt(pos, j)
            If _group(id).has_uv2 = 1 Then
                UV2Layer.IndexArray.SetAt(pos, j)
            End If
            j += 1
            pos = _group(id).indicies(n).v3 - off
            myMesh.AddPolygon(pos)
            UVDiffuseLayer.IndexArray.SetAt(pos, j)
            If _group(id).has_uv2 = 1 Then
                UV2Layer.IndexArray.SetAt(pos, j)
            End If
            n += 1
            myMesh.EndPolygon()

        Next
        Return myMesh
    End Function
    Public Function GLTF_create_primi_mesh(model_name As String, id As Integer, pManager As GLTFSdkManager) As GLTFMesh
        Dim myMesh As GLTFMesh
        myMesh = GLTFMesh.Create(pManager, model_name)

        Dim cnt = _group(id).nPrimitives_
        Dim off As UInt32
        Dim v As vect3Norm
        Dim v4 As New GLTFVector4
        Dim I As Integer
        off = _group(id).startVertex_

        '--------------------------------------------------------------------------
        '--------------------------------------------------------------------------
        'first we load all the vertices for the _group data
        myMesh.InitControlPoints(_group(id).nVertices_) ' size of array
        'add in the vertices (or control points as its called in GLTF)
        Dim cp_array(myMesh.ControlPointsCount - 1) As GLTFVector4

        For I = 0 To myMesh.ControlPointsCount - 1
            cp_array(I) = New GLTFVector4
            cp_array(I).X = _group(id).vertices(I).x
            cp_array(I).Y = _group(id).vertices(I).y
            cp_array(I).Z = _group(id).vertices(I).z
        Next

        myMesh.ControlPoints = cp_array ' push it in to the mesh object
        'create or get the layer 0
        Dim layer As GLTFLayer = myMesh.GetLayer(0)
        If layer Is Nothing Then
            myMesh.CreateLayer()
            layer = myMesh.GetLayer(0)
        End If

        '--------------------------------------------------------------------------
        '--------------------------------------------------------------------------
        'normals.. seems to be working ok
        Dim layerElementNormal = GLTFLayerElementNormal.Create(myMesh, "Normals")
        layerElementNormal.Mapping_Mode = GLTFLayerElement.MappingMode.ByPolygonVertex
        layerElementNormal.Reference_Mode = GLTFLayerElement.ReferenceMode.Direct
        'time to assign the normals to each control point.

        For I = 1 To _group(id).nPrimitives_
            Dim v1 = _group(id).indicies(I).v1
            Dim v2 = _group(id).indicies(I).v2
            Dim v3 = _group(id).indicies(I).v3
            v = unpackNormal(_group(id).vertices(v1 - off).n, _group(id).BPVT_mode)
            v4.X = v.nx
            v4.Y = v.ny
            v4.Z = v.nz
            layerElementNormal.DirectArray.Add(v4)

            v = unpackNormal(_group(id).vertices(v2 - off).n, _group(id).BPVT_mode)
            v4.X = v.nx
            v4.Y = v.ny
            v4.Z = v.nz
            layerElementNormal.DirectArray.Add(v4)

            v = unpackNormal(_group(id).vertices(v3 - off).n, _group(id).BPVT_mode)
            v4.X = v.nx
            v4.Y = v.ny
            v4.Z = v.nz
            layerElementNormal.DirectArray.Add(v4)
        Next
        layer.Normals = layerElementNormal

        '--------------------------------------------------------------------------
        'weights .. no idea how to export them from the vertex data :(
        '--------------------------------------------------------------------------
        '--------------------------------------------------------------------------
        'export vertex colors
        If _group(id).header.Contains("iii") Then ' has indices
            Dim colorLayer1 As GLTFLayerElementVertexColor = Nothing
            colorLayer1 = GLTFLayerElementVertexColor.Create(myMesh, "VertexColor")
            colorLayer1.Name = "VertexColor"
            colorLayer1.Mapping_Mode = GLTFLayerElement.MappingMode.ByControlPoint
            colorLayer1.Reference_Mode = GLTFLayerElement.ReferenceMode.Direct
            Dim color As New GLTFColor
            For I = 1 To _group(id).nPrimitives_
                Dim indi = _group(id).indicies(I)
                color.Red = CDbl(_group(id).vertices(indi.v1 - off).index_1 / 255)
                color.Green = CDbl(_group(id).vertices(indi.v1 - off).index_2 / 255)
                color.Blue = CDbl(_group(id).vertices(indi.v1 - off).index_3 / 255)
                color.Alpha = 1.0 'CDbl(_group(id).vertices(I).index_4 / 255)
                colorLayer1.DirectArray.Add(color)

                color.Red = CDbl(_group(id).vertices(indi.v2 - off).index_1 / 255)
                color.Green = CDbl(_group(id).vertices(indi.v2 - off).index_2 / 255)
                color.Blue = CDbl(_group(id).vertices(indi.v2 - off).index_3 / 255)
                color.Alpha = 1.0 'CDbl(_group(id).vertices(I).index_4 / 255)
                colorLayer1.DirectArray.Add(color)

                color.Red = CDbl(_group(id).vertices(indi.v3 - off).index_1 / 255)
                color.Green = CDbl(_group(id).vertices(indi.v3 - off).index_2 / 255)
                color.Blue = CDbl(_group(id).vertices(indi.v3 - off).index_3 / 255)
                color.Alpha = 1.0 'CDbl(_group(id).vertices(I).index_4 / 255)
                colorLayer1.DirectArray.Add(color)
            Next
            layer.VertexColors = colorLayer1
        End If

        '--------------------------------------------------------------------------
        '--------------------------------------------------------------------------
        Dim UVDiffuseLayer As GLTFLayerElementUV = GLTFLayerElementUV.Create(myMesh, "DiffuseUV")
        Dim v_2 As New GLTFVector2
        If _group(id).is_atlas_type Then
            'use UV2 mapping
            If _group(id).has_uv2 = 1 Then
                UVDiffuseLayer.Mapping_Mode = GLTFLayerElement.MappingMode.ByControlPoint
                UVDiffuseLayer.Reference_Mode = GLTFLayerElement.ReferenceMode.Direct
                layer.SetUVs(UVDiffuseLayer, GLTFLayerElement.LayerElementType.DiffuseTextures)
                For I = 0 To myMesh.ControlPointsCount - 1
                    If frmGLTF.flip_u.Checked Then
                        v_2.X = _group(id).vertices(I).u2 * -1
                    Else
                        v_2.X = _group(id).vertices(I).u2
                    End If

                    If frmGLTF.flip_v.Checked Then
                        v_2.Y = _group(id).vertices(I).v2 * -1
                    Else
                        v_2.Y = _group(id).vertices(I).v2
                    End If
                    UVDiffuseLayer.DirectArray.Add(v_2)

                Next
            End If
        Else
            'use UV1 mapping
            UVDiffuseLayer.Mapping_Mode = GLTFLayerElement.MappingMode.ByControlPoint
            UVDiffuseLayer.Reference_Mode = GLTFLayerElement.ReferenceMode.Direct
            layer.SetUVs(UVDiffuseLayer, GLTFLayerElement.LayerElementType.DiffuseTextures)
            For I = 0 To myMesh.ControlPointsCount - 1
                If frmGLTF.flip_u.Checked Then
                    v_2.X = _group(id).vertices(I).u * -1
                Else
                    v_2.X = _group(id).vertices(I).u
                End If

                If Not frmGLTF.flip_v.Checked Then
                    v_2.Y = _group(id).vertices(I).v * -1
                Else
                    v_2.Y = _group(id).vertices(I).v
                End If
                UVDiffuseLayer.DirectArray.Add(v_2)
            Next
            Debug.WriteLine(id.ToString + " : " + min_u.ToString + " : " + max_u.ToString + " : " + min_v.ToString + " : " + max_v.ToString)

            '--------------------------------------------------------------------------
        End If
        '--------------------------------------------------------------------------
        UVDiffuseLayer.IndexArray.Count = _group(id).nPrimitives_

        Dim pos As UInt32 = 0
        Dim n As UInt32 = 1
        Dim j As UInt32 = 0
        For I = 0 To _group(id).nPrimitives_ - 1
            myMesh.BeginPolygon(-1, -1, -1, False)

            j = 0
            pos = _group(id).indicies(n).v1 - off
            myMesh.AddPolygon(pos)
            UVDiffuseLayer.IndexArray.SetAt(pos, j)
            j += 1
            pos = _group(id).indicies(n).v2 - off
            myMesh.AddPolygon(pos)
            UVDiffuseLayer.IndexArray.SetAt(pos, j)
            j += 1
            pos = _group(id).indicies(n).v3 - off
            myMesh.AddPolygon(pos)
            UVDiffuseLayer.IndexArray.SetAt(pos, j)
            n += 1
            myMesh.EndPolygon()

        Next
        Return myMesh
    End Function
    Public Function GLTF_create_Vmesh(model_name As String, pManager As GLTFSdkManager, ByRef vm As v_marker_) As GLTFMesh
        Dim myMesh As GLTFMesh
        myMesh = GLTFMesh.Create(pManager, model_name)
        Dim cnt = vm.indice_count - 1
        Dim v As vec3
        Dim v4 As New GLTFVector4
        Dim I As Integer

        '--------------------------------------------------------------------------
        '--------------------------------------------------------------------------
        'first we load all the vertices for the _group data
        myMesh.InitControlPoints(vm.vertice_count) ' size of array
        'add in the vertices (or control points as its called in GLTF)
        Dim cp_array(myMesh.ControlPointsCount - 1) As GLTFVector4
        For I = 0 To myMesh.ControlPointsCount - 1
            cp_array(I) = New GLTFVector4
            cp_array(I).X = vm.vertices(I).x
            cp_array(I).Y = vm.vertices(I).y
            cp_array(I).Z = vm.vertices(I).z
        Next
        myMesh.ControlPoints = cp_array ' push it in to the mesh object
        'create or get the layer 0
        Dim layer As GLTFLayer = myMesh.GetLayer(0)
        If layer Is Nothing Then
            myMesh.CreateLayer()
            layer = myMesh.GetLayer(0)
        End If

        '--------------------------------------------------------------------------
        '--------------------------------------------------------------------------
        'normals.. seems to be working ok
        Dim layerElementNormal = GLTFLayerElementNormal.Create(myMesh, "Normals")
        layerElementNormal.Mapping_Mode = GLTFLayerElement.MappingMode.ByPolygonVertex
        layerElementNormal.Reference_Mode = GLTFLayerElement.ReferenceMode.Direct
        'time to assign the normals to each control point.

        For I = 0 To vm.indice_count - 1
            Dim v1 = vm.indices(I).a
            Dim v2 = vm.indices(I).b
            Dim v3 = vm.indices(I).c
            v = vm.normals(v1)
            v4.X = v.x
            v4.Y = v.y
            v4.Z = v.z
            layerElementNormal.DirectArray.Add(v4)

            v = vm.normals(v2)
            v4.X = v.x
            v4.Y = v.y
            v4.Z = v.z
            layerElementNormal.DirectArray.Add(v4)

            v = vm.normals(v3)
            v4.X = v.x
            v4.Y = v.y
            v4.Z = v.z
            layerElementNormal.DirectArray.Add(v4)
        Next
        layer.Normals = layerElementNormal

        '--------------------------------------------------------------------------
        'in the same way with Textures, but we are in eBY_POLYGON,
        'we should have N polygons (1 for each faces of the object)
        Dim pos As UInt32 = 0
        Dim n As UInt32 = 0
        For I = 0 To vm.indice_count - 1
            myMesh.BeginPolygon(-1, -1, -1, False)
            pos = vm.indices(n).a
            myMesh.AddPolygon(pos)

            pos = vm.indices(n).b
            myMesh.AddPolygon(pos)

            pos = vm.indices(n).c
            myMesh.AddPolygon(pos)
            n += 1
            myMesh.EndPolygon()

        Next
        Return myMesh
    End Function

    Public Function packnormalGLTF888(ByVal n As GLTFVector4) As UInt32
        'This took an entire night to get working correctly
        Try
            n.Normalize()
            n.X = Round(n.X, 4)
            n.Y = Round(n.Y, 4)
            n.Z = Round(n.Z, 4)
            Dim nx, ny, nz As Int32

            nx = s_to_int(-n.X)
            ny = s_to_int(-n.Y)
            nz = s_to_int(-n.Z)

            Dim nu = CLng(nz << 16)
            Dim nm = CLng(ny << 8)
            Dim nb = CInt(nx)
            Dim ru = Convert.ToUInt32((nu And &HFF0000) + (nm And &HFF00) + (nb And &HFF))
            Return ru
        Catch ex As Exception

        End Try
        Return New Int32
    End Function
    Public Function packnormalGLTF888_writePrimitive(ByVal n As GLTFVector4) As UInt32
        'This took an entire night to get working correctly
        Try
            n.Normalize()
            n.X = Round(n.X, 4)
            n.Y = Round(n.Y, 4)
            n.Z = Round(n.Z, 4)
            Dim nx, ny, nz As Int32

            nx = s_to_int(-n.X)
            ny = s_to_int(-n.Y)
            nz = s_to_int(-n.Z)

            Dim nu = CLng(nz << 16)
            Dim nm = CLng(ny << 8)
            Dim nb = CInt(nx)
            Dim ru = Convert.ToUInt32((nu And &HFF0000) + (nm And &HFF00) + (nb And &HFF))
            Return ru
        Catch ex As Exception

        End Try
        Return New Int32
    End Function

    Private Function unpackNormal_8_8_8(ByVal packed As UInt32) As vect3Norm
        'Console.WriteLine(packed.ToString("x"))
        Dim pkz, pky, pkx As Int32
        pkx = CLng(packed) And &HFF Xor 127
        pky = CLng(packed >> 8) And &HFF Xor 127
        pkz = CLng(packed >> 16) And &HFF Xor 127

        Dim x As Single = (pkx)
        Dim y As Single = (pky)
        Dim z As Single = (pkz)

        Dim p As New vect3Norm
        If x > 127 Then
            x = -128 + (x - 128)
        End If
        If y > 127 Then
            y = -128 + (y - 128)
        End If
        If z > 127 Then
            z = -128 + (z - 128)
        End If
        p.nx = CSng(x) / 127
        p.ny = CSng(y) / 127
        p.nz = CSng(z) / 127
        Dim len As Single = Sqrt((p.nx ^ 2) + (p.ny ^ 2) + (p.nz ^ 2))

        'avoid division by 0
        If len = 0.0F Then len = 1.0F
        'len = 1.0
        'reduce to unit size
        p.nx = -(p.nx / len)
        p.ny = -(p.ny / len)
        p.nz = -(p.nz / len)
        'Console.WriteLine(p.x.ToString("0.000000") + " " + p.y.ToString("0.000000") + " " + p.z.ToString("0.000000"))
        Return p
    End Function


    Private Function unpackNormal(ByVal packed As UInt32, type As Boolean) As vect3Norm
        If type Then
            Return unpackNormal_8_8_8(packed)
        End If
        Dim pkz, pky, pkx As Int32
        pkz = packed And &HFFC00000
        pky = packed And &H4FF800
        pkx = packed And &H7FF

        Dim z As Int32 = pkz >> 22
        Dim y As Int32 = (pky << 10L) >> 21
        Dim x As Int32 = (pkx << 21L) >> 21
        Dim p As New vect3Norm

        p.nx = CSng(x) / 1023.0!
        p.ny = CSng(y) / 1023.0!
        p.nz = CSng(z) / 511.0!
        Dim len As Single = Sqrt((p.nx ^ 2) + (p.ny ^ 2) + (p.nz ^ 2))

        'avoid division by 0
        If len = 0.0F Then len = 1.0F

        'reduce to unit size
        p.nx = (p.nx / len)
        p.ny = (p.ny / len)
        p.nz = (p.nz / len)
        Return p
    End Function

#End Region

End Module
