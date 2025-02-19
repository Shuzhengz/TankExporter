﻿Imports System.Windows
Imports System.Windows.Forms
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Net
Imports System.Text
Imports System.IO
Imports System.Xml
Imports System.Web
Imports Tao.OpenGl
Imports Tao.Platform.Windows
Imports Tao.FreeGlut
Imports Tao.FreeGlut.Glut
Imports Tao.DevIl
Imports Microsoft.VisualBasic.Strings
Imports System.Math
Imports System.Object
Imports System.Threading
Imports System.Data
Imports Skill.FbxSDK
Imports Skill.FbxSDK.IO
Imports cttools
Imports System.Windows.Forms.VisualStyles
Imports Microsoft.VisualBasic.Devices
Imports System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock
Module modFBX
    Public t_fbx(0) As _grps
    Public FBX_Texture_path As String
    Public fbxgrp(0) As _grps
    Public ctz As New cttools.norm_utilities
    Public FBX_LOADED As Boolean = False
    Public m_groups() As mgrp_
    Public fbx_uv2s(100000) As uv_
    Public uv2_total_count As Integer = 0
    Public Structure mgrp_
        Public group_list() As Integer
        Public list() As Integer
        Public existingCount As Integer
        Public m_type As Integer
        Public cnt As Integer
        Public f_name() As String
        Public section_names() As String
        Public package_id() As Integer
        Public changed As Boolean
        Public new_objects As Boolean
    End Structure
    Public Sub remove_loaded_fbx()
        'If FBX_LOADED Then
        FBX_LOADED = False
        For ii = 1 To fbxgrp.Length - 2
            Gl.glDeleteTextures(1, fbxgrp(ii).color_Id)
            Gl.glFinish()
            Gl.glDeleteTextures(1, fbxgrp(ii).color_Id)
            Gl.glFinish()
            Gl.glDeleteLists(fbxgrp(ii).call_list, 1)
            Gl.glDeleteLists(fbxgrp(ii).vertex_pick_list, 1)
            Gl.glFinish()
        Next
        frmMain.m_show_fbx.Visible = False
        frmMain.m_show_fbx.Checked = False
        frmMain.m_show_fbx.Enabled = True
        frmMain.m_write_non_tank_primitive.Enabled = False

        ReDim fbxgrp(0)
        GC.Collect() 'clean up garbage
        GC.WaitForFullGCComplete()
        'End If
    End Sub

    Private Sub displayMatrix(m As FbxXMatrix, ByVal name As String)

        Console.WriteLine(name + "------------------------------------------")
        For i = 0 To 3
            For j = 0 To 3
                Console.Write(Round(m.Get(j, i), 6).ToString + vbTab + vbTab)
            Next
            Console.Write(vbCrLf)
        Next

        Console.Write(vbCrLf)

    End Sub
    Public Sub purge_fbx()

    End Sub


    Public Sub import_FBX()
        'fbx import sub
        Dim j As UInt32 = 0
        Dim i As UInt32 = 0
        Dim start_index As Integer = 0
        Dim start_vertex As Integer = 0

        frmMain.OpenFileDialog1.InitialDirectory = My.Settings.fbx_path
        frmMain.OpenFileDialog1.Filter = "AutoDesk (*.FBX)|*.fbx"
        frmMain.OpenFileDialog1.Title = "Import FBX..."
        If frmMain.OpenFileDialog1.FileName = "OpenFileDialog1" Then
            frmMain.OpenFileDialog1.FileName = ""
        End If
        If Not frmMain.OpenFileDialog1.ShowDialog = Forms.DialogResult.OK Then
            Return
        End If
        frmComponentView.clear_fbx_list()
        frmReverseVertexWinding.clear_group_list()

        My.Settings.fbx_path = Path.GetDirectoryName(frmMain.OpenFileDialog1.FileName)
        My.Settings.Save()
        frmMain.clean_house()
        remove_loaded_fbx()
        frmMain.info_Label.Text = frmMain.OpenFileDialog1.FileName
        Application.DoEvents()
        Application.DoEvents()
        Application.DoEvents()
        frmMain.pb1.Visible = True
        Application.DoEvents()

        Dim pManager As FbxSdkManager
        Dim scene As FbxScene
        pManager = FbxSdkManager.Create
        scene = FbxScene.Create(pManager, "My Scene")
        Dim fileformat As Integer = Skill.FbxSDK.IO.FileFormat.FbxAscii
        'Detect the file format of the file to be imported            
        Dim filename = frmMain.OpenFileDialog1.FileName
        If Not pManager.IOPluginRegistry.DetectFileFormat(filename, fileformat) Then

            ' Unrecognizable file format.
            ' Try to fall back to SDK's native file format (an FBX binary file).
            fileformat = pManager.IOPluginRegistry.NativeReaderFormat
        End If

        Dim importOptions = Skill.FbxSDK.IO.FbxStreamOptionsFbxReader.Create(pManager, "")
        Dim importer As Skill.FbxSDK.IO.FbxImporter = Skill.FbxSDK.IO.FbxImporter.Create(pManager, "")

        importer.FileFormat = fileformat    ' get file format
        Dim imp_status As Boolean = importer.Initialize(filename)
        If Not imp_status Then
            MsgBox("Failed to open " + frmMain.OpenFileDialog1.FileName, MsgBoxStyle.Exclamation, "FBX Load Error...")
            pManager.Destroy()
            GoTo outofhere
        End If
        If Not importer.IsFBX Then
            MsgBox("Are you sure this is a FBX file? " + vbCrLf + frmMain.OpenFileDialog1.FileName, MsgBoxStyle.Exclamation, "FBX Load Error...")
            pManager.Destroy()
            GoTo outofhere
        End If
        importOptions.SetOption(Skill.FbxSDK.IO.FbxStreamOptionsFbx.MATERIAL, True)
        importOptions.SetOption(Skill.FbxSDK.IO.FbxStreamOptionsFbx.TEXTURE, True)
        importOptions.SetOption(Skill.FbxSDK.IO.FbxStreamOptionsFbx.LINK, False)
        importOptions.SetOption(Skill.FbxSDK.IO.FbxStreamOptionsFbx.SHAPE, False)
        importOptions.SetOption(Skill.FbxSDK.IO.FbxStreamOptionsFbx.GOBO, False)
        importOptions.SetOption(Skill.FbxSDK.IO.FbxStreamOptionsFbx.ANIMATION, False)
        importOptions.SetOption(Skill.FbxSDK.IO.FbxStreamOptionsFbx.GLOBAL_SETTINGS, False)

        imp_status = importer.Import(scene, importOptions)

        Dim rootnode As FbxNode = scene.RootNode

        Dim p As FbxProperty = rootnode.GetFirstProperty

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

        Dim childnode As FbxNode
        Dim mesh As FbxMesh = Nothing
        LOADING_FBX = True ' so we dont read from the res_Mods folder
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
        process_fbx_data()
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
        For i = 1 To fbxgrp.Length - 1
            Dim id = Gl.glGenLists(1)
            Gl.glNewList(id, Gl.GL_COMPILE)
            fbxgrp(i).call_list = id
            make_fbx_display_lists(fbxgrp(i).nPrimitives_ * 3, i)
            Gl.glEndList()
        Next
        FBX_LOADED = True
        frmMain.info_Label.Visible = False
        frmMain.m_show_fbx.Checked = True
        If MODEL_LOADED Then
            frmMain.m_show_fbx.Visible = True
        End If
        LOADING_FBX = False ' so we dont read from the res_Mods folder

    End Sub
    Public Sub import_primitives_FBX()
        'fbx import sub
        Dim j As UInt32 = 0
        Dim i As UInt32 = 0
        Dim start_index As Integer = 0
        Dim start_vertex As Integer = 0
        Dim tfp As String = "C:\"
        If File.Exists(Temp_Storage + "\Fbx_Primi_in_folder.txt") Then
            tfp = File.ReadAllText(Temp_Storage + "\Fbx_Primi_in_folder.txt")
        End If
        frmMain.OpenFileDialog1.InitialDirectory = tfp
        frmMain.OpenFileDialog1.Filter = "AutoDesk (*.FBX)|*.fbx"
        frmMain.OpenFileDialog1.Title = "Import PRIMITIVES FBX..."
        If frmMain.OpenFileDialog1.FileName = "OpenFileDialog1" Then
            frmMain.OpenFileDialog1.FileName = ""
        End If
        If Not frmMain.OpenFileDialog1.ShowDialog = Forms.DialogResult.OK Then
            If Not PRIMITIVES_MODE Then
                frmMain.m_load_textures.Enabled = True
            End If
            Return
        End If
        File.WriteAllText(Temp_Storage + "\Fbx_Primi_in_folder.txt", Path.GetDirectoryName(frmMain.OpenFileDialog1.FileName))
        ReDim UV2s(100000)
        uv2_total_count = 0

        frmComponentView.clear_fbx_list()
        frmReverseVertexWinding.clear_group_list()

        My.Settings.fbx_path = Path.GetDirectoryName(frmMain.OpenFileDialog1.FileName)
        frmMain.clean_house()
        remove_loaded_fbx()
        'frmMain.info_Label.Visible = True
        frmMain.info_Label.Text = frmMain.OpenFileDialog1.FileName
        Application.DoEvents()
        Application.DoEvents()
        Application.DoEvents()
        frmMain.pb1.Visible = True
        Application.DoEvents()

        Dim pManager As FbxSdkManager
        Dim scene As FbxScene
        pManager = FbxSdkManager.Create
        scene = FbxScene.Create(pManager, "My Scene")
        Dim fileformat As Integer = Skill.FbxSDK.IO.FileFormat.FbxAscii
        'Detect the file format of the file to be imported            
        Dim filename = frmMain.OpenFileDialog1.FileName
        If Not pManager.IOPluginRegistry.DetectFileFormat(filename, fileformat) Then
            ' Unrecognizable file format.
            ' Try to fall back to SDK's native file format (an FBX binary file).
            fileformat = pManager.IOPluginRegistry.NativeReaderFormat
        End If

        Dim importOptions = Skill.FbxSDK.IO.FbxStreamOptionsFbxReader.Create(pManager, "")
        Dim importer As Skill.FbxSDK.IO.FbxImporter = Skill.FbxSDK.IO.FbxImporter.Create(pManager, "")

        importer.FileFormat = fileformat    ' get file format
        Dim imp_status As Boolean = importer.Initialize(filename)
        If Not imp_status Then
            MsgBox("Failed to open " + frmMain.OpenFileDialog1.FileName, MsgBoxStyle.Exclamation, "FBX Load Error...")
            pManager.Destroy()
            GoTo outofhere
        End If
        If Not importer.IsFBX Then
            MsgBox("Are you sure this is a FBX file? " + vbCrLf + frmMain.OpenFileDialog1.FileName, MsgBoxStyle.Exclamation, "FBX Load Error...")
            pManager.Destroy()
            GoTo outofhere
        End If
        importOptions.SetOption(Skill.FbxSDK.IO.FbxStreamOptionsFbx.MATERIAL, True)
        importOptions.SetOption(Skill.FbxSDK.IO.FbxStreamOptionsFbx.TEXTURE, True)
        importOptions.SetOption(Skill.FbxSDK.IO.FbxStreamOptionsFbx.LINK, False)
        importOptions.SetOption(Skill.FbxSDK.IO.FbxStreamOptionsFbx.SHAPE, False)
        importOptions.SetOption(Skill.FbxSDK.IO.FbxStreamOptionsFbx.GOBO, False)
        importOptions.SetOption(Skill.FbxSDK.IO.FbxStreamOptionsFbx.ANIMATION, False)
        importOptions.SetOption(Skill.FbxSDK.IO.FbxStreamOptionsFbx.GLOBAL_SETTINGS, False)

        imp_status = importer.Import(scene, importOptions)

        Dim rootnode As FbxNode = scene.RootNode

        Dim p As FbxProperty = rootnode.GetFirstProperty

        Dim sc = rootnode.Scaling.GetValueAsDouble3
        While 1
            p = rootnode.GetNextProperty(p)
            If Not p.IsValid Then Exit While
        End While

        Dim tankComponentCount As Int32 = 0
        Dim TboneCount As Int32 = 0
        'make room for the mesh data
        Dim cnt As Integer = 0

        Dim childnode As FbxNode
        Dim mesh As FbxMesh = Nothing
        LOADING_FBX = True ' so we dont read from the res_Mods folder
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
        ReDim m_groups(2).list(fbxgrp.Length)
        m_groups(2).cnt = fbxgrp.Length - 1
        For i = 1 To fbxgrp.Length - 1
            m_groups(2).list(i - 1) = i
            frmComponentView.add_to_fbx_list(i, fbxgrp(i).name)
            frmReverseVertexWinding.add_to_fbx_list(i, fbxgrp(i).name)
            Dim id = Gl.glGenLists(1)
            fbxgrp(i).visible = True
            fbxgrp(i).component_visible = True
            fbxgrp(i).reverse_winding = False
            Gl.glNewList(id, Gl.GL_COMPILE)
            fbxgrp(i).call_list = id
            make_fbx_display_lists(fbxgrp(i).nPrimitives_ * 3, i)
            Gl.glEndList()
        Next
        m_groups(2).changed = True
        m_groups(2).new_objects = False
        m_groups(2).m_type = 2 'hull no flipping or anything
        FBX_LOADED = True
        MODEL_LOADED = True
        frmMain.info_Label.Visible = False
        frmMain.m_show_fbx.Checked = True
        frmMain.m_show_fbx.Visible = True
        frmMain.m_show_fbx.Checked = True
        frmMain.m_show_fbx.Enabled = False
        frmMain.m_hide_show_components.Enabled = True
        frmMain.m_set_vertex_winding_order.Enabled = True
        LOADING_FBX = False ' so we dont read from the res_Mods folder
        PRIMITIVES_MODE = True
        frmMain.m_write_non_tank_primitive.Enabled = True
    End Sub

    Public Sub export_fbx()
        'export FBX
        Dim rootNode As FbxNode
        Dim id As Integer
        Dim model_name As String = ""
        Dim mat_main As String = ""
        Dim mat_NM As String = ""
        Dim mat_uv2 As String = ""
        Dim fbx_locaction As String = My.Settings.fbx_path
        Dim rp As String = Application.StartupPath
        Dim _date As String = Date.Now
        Dim ar = _date.Split(" ")
        _date = ar(0) + " " + ar(1) + ".0"


        Dim vert_string, normal_string, uv1_string, uv2_string, uv_index, indices_string As New StringBuilder

        'Tried everything so lets do it the hard way
        '--------------------------------------------------------------------------
        Dim m_name As String = "Material"
        Dim s_name As String = "Phong"
        Dim EmissiveColor = New FbxDouble3(0.0, 0.0, 0.0)
        Dim AmbientColor = New FbxDouble3(0.9, 0.9, 0.9)
        Dim SpecularColor = New FbxDouble3(0.7, 0.7, 0.7)
        Dim DiffuseColor As New FbxDouble3(0.8, 0.8, 0.8)
        '--------------------------------------------------------------------------
        Dim pManager As FbxSdkManager
        pManager = FbxSdkManager.Create
        'create the material and texture arrays.

        Dim texture_count = textures.Length
        Dim lMaterials(texture_count) As FbxSurfacePhong
        Dim vMaterials(5)
        Dim lTextures(texture_count) As FbxTexture
        Dim lTextures_N(texture_count) As FbxTexture
        'make the materials
        For i = 0 To texture_count - 1
            lMaterials(i) = fbx_create_material(pManager, i) 'Material
            lTextures(i) = fbx_create_texture(pManager, i) 'Color Map
            lTextures_N(i) = fbx_create_texture_N(pManager, i) 'Normal Map
        Next
        'make v materials
        For i = 0 To 5
            vMaterials(i) = fbx_create_Vmaterial(pManager, i)
        Next
        'create manager and scene
        Dim scene As FbxScene
        scene = FbxScene.Create(pManager, file_name)
        scene.SceneInfo.Author = "Exported using Coffee_'s Tank Exporter tool"
        scene.SceneInfo.Comment = TANK_NAME

        frmFBX.Label1.Visible = False
        Dim node_list() = {FbxNode.Create(pManager, model_name)}
        Dim node_Vlist() = {FbxNode.Create(pManager, "pin")}
        '--------------------------------------------------------------------------
        rootNode = scene.RootNode
        rootNode.CreateTakeNode("Show all faces")
        rootNode.SetCurrentTakeNode("Show all faces")


        Dim dfr = New FbxVector4(0.0, 0.0, 0.0, 0.0)
        Dim dfs = New FbxVector4(1.0, 1.0, 1.0, 0.0)
        Dim dft As New FbxVector4(0.0, 0.0, 0.0, 1.0)
        rootNode.SetDefaultR(dfr)
        rootNode.SetDefaultS(dfs)
        rootNode.SetDefaultT(dft)
        'add the markers to the root
        ' get total vNodes needed
        Dim cnt As Integer = 0



        For id = 1 To object_count
            ReDim Preserve node_list(id + 1)

            mat_main = FBX_Texture_path + "\" + Path.GetFileNameWithoutExtension(_group(id).color_name) + ".png"
            mat_NM = FBX_Texture_path + "\" + Path.GetFileNameWithoutExtension(_group(id).normal_name) + ".png"
            mat_uv2 = _group(id).detail_name

            model_name = _group(id).name.Replace("/", "\")
            model_name = model_name.Replace(":", "~")
            model_name = model_name.Replace("vehicles\", "")
            model_name = model_name.Replace("primitives_processed", "pri")
            model_name = model_name.Replace("\lod0\", "\l\")
            node_list(id) = FbxNode.Create(pManager, model_name)

            'create mesh node
            Dim mymesh = fbx_create_mesh(model_name, id, pManager)

            'Dim m As New FbxXMatrix
            Dim m_ = _object(id).matrix

            Dim scale As New SharpDX.Vector3
            Dim rot As New SharpDX.Quaternion
            Dim trans As New SharpDX.Vector3
            Dim Mt As New SharpDX.Matrix
            Mt = load_matrix_decompose(m_, trans, scale, rot)

            Dim r_vector As New FbxVector4(rot.X, 0.0, rot.Z, rot.W)
            Dim t_vector As New FbxVector4(-trans.X, trans.Y, trans.Z)
            Dim s_vector As New FbxVector4(-scale.X, scale.Y, scale.Z, 0.0)

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
            Dim layercontainer As FbxLayerContainer = mymesh
            Dim layerElementTexture As FbxLayerElementTexture = layercontainer.GetLayer(0).DiffuseTextures
            Dim layerElementNTexture As FbxLayerElementTexture = layercontainer.GetLayer(0).BumpTextures
            If layerElementTexture Is Nothing Then
                layerElementTexture = FbxLayerElementTexture.Create(layercontainer, "diffuseMap")
                layercontainer.GetLayer(0).DiffuseTextures = layerElementTexture
                layerElementNTexture = FbxLayerElementTexture.Create(layercontainer, "normalMap")
                layercontainer.GetLayer(0).BumpTextures = layerElementNTexture
            End If
            'not 100% sure about the translucent but it isn't breaking anything.
            layerElementTexture.Blend_Mode = FbxLayerElementTexture.BlendMode.Translucent
            layerElementTexture.Alpha = 1.0
            layerElementTexture.Mapping_Mode = FbxLayerElement.MappingMode.AllSame
            layerElementTexture.Reference_Mode = FbxLayerElement.ReferenceMode.Direct

            layerElementNTexture.Blend_Mode = FbxLayerElementTexture.BlendMode.Translucent
            layerElementNTexture.Alpha = 1.0
            layerElementNTexture.Mapping_Mode = FbxLayerElement.MappingMode.AllSame
            layerElementNTexture.Reference_Mode = FbxLayerElement.ReferenceMode.Direct

            'add the texture from the texture array using the Texture ID for this mesh section
            layerElementTexture.DirectArray.Add(lTextures(_group(id).texture_id))
            layerElementNTexture.DirectArray.Add(lTextures_N(_group(id).texture_id))
            node_list(id).NodeAttribute = mymesh

            node_list(id).SetDefaultR(r_vector)
            node_list(id).SetDefaultT(t_vector)
            node_list(id).SetDefaultS(s_vector)

            If node_list(id).IsValid And frmFBX.export_textures.Checked Then ' useless test but Im leaving it.
                'add the texture from the array using this models texture ID
                node_list(id).AddMaterial(lMaterials(_group(id).texture_id))
                '---------------------------------------
                'If we dont connect this texture to this node, it will never show up!
                node_list(id).ConnectSrcObject(lMaterials(_group(id).texture_id), FbxConnectionType.ConnectionDefault)
            End If
            node_list(id).Shading_Mode = FbxNode.ShadingMode.TextureShading ' not even sure this is needed but what ever.
            Dim estr = pManager.LastErrorString
            Dim vstr = mymesh.LastErrorString
            Dim vmm = node_list(id).LastErrorString

            rootNode.AddChild(node_list(id))
            rootNode.ConnectSrcObject(node_list(id), FbxConnectionType.ConnectionDefault)

we_dont_want_this_one_saved:
        Next 'Id

        'time to save... not sure im even close to having what i need to save but fuck it!
        Dim exporter As Skill.FbxSDK.IO.FbxExporter = FbxExporter.Create(pManager, "")
        If Not exporter.Initialize(frmMain.SaveFileDialog1.FileName) Then
            MsgBox("fbx unable to initialize exporter!", MsgBoxStyle.Exclamation, "FBX Error..")
            GoTo outahere
        End If
        Dim version As Version = Skill.FbxSDK.IO.FbxIO.CurrentVersion
        Console.Write(String.Format("FBX version number for this FBX SDK is {0}.{1}.{2}",
                          version.Major, version.Minor, version.Revision))
        If frmFBX.export_as_binary_cb.Checked Then
            exporter.FileFormat = IO.FileFormat.FbxBinary
        Else
            exporter.FileFormat = IO.FileFormat.FbxAscii
        End If

        Dim exportOptions As Skill.FbxSDK.IO.FbxStreamOptionsFbxWriter _
                = Skill.FbxSDK.IO.FbxStreamOptionsFbxWriter.Create(pManager, "")
        If pManager.IOPluginRegistry.WriterIsFBX(IO.FileFormat.FbxAscii) Then

            ' Export options determine what kind of data is to be imported.
            ' The default (except for the option eEXPORT_TEXTURE_AS_EMBEDDED)
            ' is true, but here we set the options explictly.
            exportOptions.SetOption(Skill.FbxSDK.IO.FbxStreamOptionsFbx.MATERIAL, True)
            exportOptions.SetOption(Skill.FbxSDK.IO.FbxStreamOptionsFbx.TEXTURE, True)
            exportOptions.SetOption(Skill.FbxSDK.IO.FbxStreamOptionsFbx.EMBEDDED, False)
            exportOptions.SetOption(Skill.FbxSDK.IO.FbxStreamOptionsFbx.LINK, True)
            exportOptions.SetOption(Skill.FbxSDK.IO.FbxStreamOptionsFbx.SHAPE, False)
            exportOptions.SetOption(Skill.FbxSDK.IO.FbxStreamOptionsFbx.GOBO, False)
            exportOptions.SetOption(Skill.FbxSDK.IO.FbxStreamOptionsFbx.ANIMATION, False)
            exportOptions.SetOption(Skill.FbxSDK.IO.FbxStreamOptionsFbx.GLOBAL_SETTINGS, False)
            exportOptions.SetOption(Skill.FbxSDK.IO.FbxStreamOptionsFbx.MEDIA, False)
        End If
        Dim status = exporter.Export(scene, exportOptions)
        exporter.Destroy()
        pManager.Destroy()

outahere:
        frmFBX.Label1.Visible = True

    End Sub
    Public Sub export_primitives_fbx()
        'export FBXprimitives_
        Dim rootNode As FbxNode
        Dim id As Integer
        Dim model_name As String = ""
        Dim mat_main As String = ""
        Dim mat_NM As String = ""
        Dim mat_uv2 As String = ""
        Dim fbx_locaction As String = My.Settings.fbx_path
        Dim rp As String = Application.StartupPath
        Dim _date As String = Date.Now
        Dim ar = _date.Split(" ")
        _date = ar(0) + " " + ar(1) + ".0"


        Dim vert_string, normal_string, uv1_string, uv2_string, uv_index, indices_string As New StringBuilder

        'Tried everything so lets do it the hard way
        '--------------------------------------------------------------------------
        Dim pManager As FbxSdkManager
        pManager = FbxSdkManager.Create
        'create the material and texture arrays.

        Dim texture_count = object_count
        Dim lMaterials(texture_count) As FbxSurfacePhong
        Dim lTextures(texture_count) As FbxTexture
        Dim lTextures_N(texture_count) As FbxTexture
        'make the materials
        For i = 1 To texture_count
            lMaterials(i) = fbx_create_material(pManager, i) 'Material
            lTextures(i) = fbx_create_texture_primtive(pManager, i) 'Color Map
            lTextures_N(i) = fbx_create_texture_N_primitive(pManager, i) 'Normal Map
        Next
        'create manager and scene
        Dim scene As FbxScene
        scene = FbxScene.Create(pManager, file_name)
        scene.SceneInfo.Author = "Exported using Coffee_'s Tank Exporter tool"
        scene.SceneInfo.Comment = TANK_NAME
        'scene.CreateTake("Show all faces")
        'scene.SetCurrentTake("Show all faces")

        frmFBX.Label1.Visible = False
        Dim node_list() = {FbxNode.Create(pManager, model_name)}
        Dim node_Vlist() = {FbxNode.Create(pManager, "pin")}
        '--------------------------------------------------------------------------
        rootNode = scene.RootNode
        rootNode.CreateTakeNode("Show all faces")
        rootNode.SetCurrentTakeNode("Show all faces")


        Dim dfr = New FbxVector4(0.0, 0.0, 0.0, 0.0)
        Dim dfs = New FbxVector4(1.0, 1.0, 1.0, 0.0)
        Dim dft As New FbxVector4(0.0, 0.0, 0.0, 1.0)
        rootNode.SetDefaultR(dfr)
        rootNode.SetDefaultS(dfs)
        rootNode.SetDefaultT(dft)
        'add the markers to the root
        ' get total vNodes needed
        Dim cnt As Integer = 0

        For id = 1 To object_count
            ReDim Preserve node_list(id + 1)

            model_name = FBX_NAME + "~" + id.ToString
            node_list(id) = FbxNode.Create(pManager, model_name)
            'create mesh node
            Dim mymesh = fbx_create_primi_mesh(model_name, id, pManager)

            Dim m_ = _object(id).matrix

            Dim scale As New SharpDX.Vector3
            Dim rot As New SharpDX.Quaternion
            Dim trans As New SharpDX.Vector3
            Dim Mt As New SharpDX.Matrix
            Mt = load_matrix_decompose(m_, trans, scale, rot)


            Dim r_vector As New FbxVector4(rot.X, 0.0, rot.Z, rot.W)
            Dim t_vector As New FbxVector4(-trans.X, trans.Y, trans.Z)
            Dim s_vector As New FbxVector4(-scale.X, scale.Y, scale.Z, 0.0)

            Dim layercontainer As FbxLayerContainer = mymesh

            If _group(id).is_atlas_type Then
            End If

            Dim layerElementTexture As FbxLayerElementTexture = layercontainer.GetLayer(0).DiffuseTextures
            Dim layerElementNTexture As FbxLayerElementTexture = layercontainer.GetLayer(0).BumpTextures

            If layerElementTexture Is Nothing Then
                layerElementTexture = FbxLayerElementTexture.Create(layercontainer, "diffuseMap")
                layercontainer.GetLayer(0).DiffuseTextures = layerElementTexture
                layerElementNTexture = FbxLayerElementTexture.Create(layercontainer, "normalMap")
                layercontainer.GetLayer(0).BumpTextures = layerElementNTexture
            End If

            'not 100% sure about the translucent but it isn't breaking anything.
            layerElementTexture.Blend_Mode = FbxLayerElementTexture.BlendMode.Translucent
            layerElementTexture.Alpha = 1.0
            layerElementTexture.Mapping_Mode = FbxLayerElement.MappingMode.AllSame
            layerElementTexture.Reference_Mode = FbxLayerElement.ReferenceMode.Direct

            layerElementNTexture.Blend_Mode = FbxLayerElementTexture.BlendMode.Translucent
            layerElementNTexture.Alpha = 1.0
            layerElementNTexture.Mapping_Mode = FbxLayerElement.MappingMode.AllSame
            layerElementNTexture.Reference_Mode = FbxLayerElement.ReferenceMode.Direct

            'add the texture from the texture array using the Texture ID for this mesh section
            layerElementTexture.DirectArray.Add(lTextures(_group(id).fbx_texture_id))
            layerElementNTexture.DirectArray.Add(lTextures_N(_group(id).fbx_N_texture_id))
            'add the texture from the texture array using the Texture ID for this mesh section
            node_list(id).NodeAttribute = mymesh
            Dim dr, ds, dt As New FbxVector4
            dr.Set(0, 0, 0, 0)
            ds.Set(1, 1, 1, 1)
            dt.Set(0, 0, 0, 1)
            node_list(id).SetDefaultR(r_vector)
            node_list(id).SetDefaultT(t_vector)
            node_list(id).SetDefaultS(s_vector)

            If node_list(id).IsValid And frmFBX.export_textures.Checked Then ' useless test but Im leaving it.
                'add the texture from the array using this models texture ID
                Try
                    node_list(id).AddMaterial(lMaterials(_group(id).texture_id))

                    '---------------------------------------
                    'If we dont connect this texture to this node, it will never show up!
                    node_list(id).ConnectSrcObject(lMaterials(id), FbxConnectionType.ConnectionDefault)
                Catch ex As Exception

                End Try
            End If
            node_list(id).Shading_Mode = FbxNode.ShadingMode.TextureShading ' not even sure this is needed but what ever.
            Dim estr = pManager.LastErrorString
            Dim vstr = mymesh.LastErrorString
            Dim vmm = node_list(id).LastErrorString
            'Debug.WriteLine(id.ToString("000") + ":--------")
            'Debug.WriteLine(estr)
            'Debug.WriteLine(vstr)
            'Debug.WriteLine(vmm)

            rootNode.AddChild(node_list(id))
            rootNode.ConnectSrcObject(node_list(id), FbxConnectionType.ConnectionDefault)

we_dont_want_this_one_saved:
        Next 'Id



        'time to save... not sure im even close to having what i need to save but fuck it!
        Dim exporter As Skill.FbxSDK.IO.FbxExporter = FbxExporter.Create(pManager, "")
        If Not exporter.Initialize(frmMain.SaveFileDialog1.FileName) Then
            MsgBox("fbx unable to initialize exporter!", MsgBoxStyle.Exclamation, "FBX Error..")
            GoTo outahere
        End If
        Dim version As Version = Skill.FbxSDK.IO.FbxIO.CurrentVersion
        Console.Write(String.Format("FBX version number for this FBX SDK is {0}.{1}.{2}",
                          version.Major, version.Minor, version.Revision))
        If frmFBX.export_as_binary_cb.Checked Then
            exporter.FileFormat = IO.FileFormat.FbxBinary
        Else
            exporter.FileFormat = IO.FileFormat.FbxAscii
        End If

        Dim exportOptions As Skill.FbxSDK.IO.FbxStreamOptionsFbxWriter _
                = Skill.FbxSDK.IO.FbxStreamOptionsFbxWriter.Create(pManager, "")
        If pManager.IOPluginRegistry.WriterIsFBX(IO.FileFormat.FbxAscii) Then

            ' Export options determine what kind of data is to be imported.
            ' The default (except for the option eEXPORT_TEXTURE_AS_EMBEDDED)
            ' is true, but here we set the options explictly.
            exportOptions.SetOption(Skill.FbxSDK.IO.FbxStreamOptionsFbx.MATERIAL, True)
            exportOptions.SetOption(Skill.FbxSDK.IO.FbxStreamOptionsFbx.TEXTURE, True)
            exportOptions.SetOption(Skill.FbxSDK.IO.FbxStreamOptionsFbx.EMBEDDED, False)
            exportOptions.SetOption(Skill.FbxSDK.IO.FbxStreamOptionsFbx.LINK, True)
            exportOptions.SetOption(Skill.FbxSDK.IO.FbxStreamOptionsFbx.SHAPE, False)
            exportOptions.SetOption(Skill.FbxSDK.IO.FbxStreamOptionsFbx.GOBO, False)
            exportOptions.SetOption(Skill.FbxSDK.IO.FbxStreamOptionsFbx.ANIMATION, False)
            exportOptions.SetOption(Skill.FbxSDK.IO.FbxStreamOptionsFbx.GLOBAL_SETTINGS, False)
            exportOptions.SetOption(Skill.FbxSDK.IO.FbxStreamOptionsFbx.MEDIA, False)
        End If
        Dim status = exporter.Export(scene, exportOptions)
        exporter.Destroy()
        pManager.Destroy()
        'textureAmbientLayer.Destroy()
        'textureDiffuseLayer.Destroy()
outahere:
        frmFBX.Label1.Visible = True

    End Sub

#Region "Import helpers"


    Private Function readMeshdata(ByVal i As Integer, ByRef childnode As FbxNode,
                                  start_vertex As Integer, start_index As Integer,
                                  scene As FbxScene, rootnode As FbxNode, mesh As FbxMesh)

        ReDim Preserve fbxgrp(i)

        fbxgrp(i).name = childnode.NameOnly
        If Not fbxgrp(i).name.Contains("vehicles\") And (fbxgrp(i).name.Contains("\l\") Or fbxgrp(i).name.Contains("\lod0\")) Then
            fbxgrp(i).name = "vehicles\" + childnode.NameOnly
            fbxgrp(i).name = fbxgrp(i).name.Replace("pri~", "primitives_processed~")
            fbxgrp(i).name = fbxgrp(i).name.Replace("primitives~", "primitives_processed~")
            fbxgrp(i).name = fbxgrp(i).name.Replace("\l\", "\lod0\")
        End If
        'get transform information -------------------------------------
        Dim fbx_matrix As New FbxXMatrix
        fbxgrp(i).rotation = New FbxVector4
        fbxgrp(i).translation = New FbxVector4
        fbxgrp(i).scale = New FbxVector4
        fbxgrp(i).scale.X = 1.0
        fbxgrp(i).scale.Y = 1.0
        fbxgrp(i).scale.Z = 1.0

        Dim t As New FbxTime
        Dim GlobalUnitScale = scene.GlobalSettings.FindProperty("UnitScaleFactor", False).GetValueAsDouble

        Dim ls = childnode.GetLocalSFromDefaultTake(FbxNode.PivotSet.SourceSet)
        If ls.X = 1.0 Then
            ls.X = 0.1
            ls.Y = 0.1
            ls.Z = 1.0
        End If

        Dim nodeGT = rootnode.GetGlobalFromDefaultTake(FbxNode.PivotSet.DestinationSet)

        Dim lr = childnode.GetLocalRFromDefaultTake(FbxNode.PivotSet.SourceSet)
        Dim lt = childnode.GetLocalTFromCurrentTake(t)
        Dim gr = childnode.Parent.GetLocalRFromCurrentTake(t)

        Dim scaling = childnode.Scaling.GetValueAsDouble3

        fbxgrp(i).rotation = childnode.GetGeometricRotation(FbxNode.PivotSet.SourceSet)
        fbxgrp(i).translation = childnode.GetGeometricTranslation(FbxNode.PivotSet.SourceSet)
        fbxgrp(i).scale = childnode.GetGeometricScaling(FbxNode.PivotSet.SourceSet)
        fbx_matrix.SetIdentity()

        Dim dr As New FbxVector4
        Dim dt As New FbxVector4
        Dim ds As New FbxVector4

        Dim gm = childnode.GetGlobalFromCurrentTake(t)

        childnode.GetDefaultR(dr)
        childnode.GetDefaultS(ds)
        childnode.GetDefaultT(dt)
        fbxgrp(i).rotation = childnode.GetGeometricRotation(FbxNode.PivotSet.SourceSet)
        fbxgrp(i).translation = childnode.GetGeometricTranslation(FbxNode.PivotSet.SourceSet)
        fbxgrp(i).scale = childnode.GetGeometricScaling(FbxNode.PivotSet.SourceSet)
        Dim TnR As Double = 0
        Try
            TnR = Round(fbxgrp(i).rotation.X, 6) + Round(fbxgrp(i).rotation.Y, 6) + Round(fbxgrp(i).rotation.Z, 6) _
                + Round(fbxgrp(i).translation.X, 6) + Round(fbxgrp(i).translation.Y, 6) + Round(fbxgrp(i).translation.Z, 6)
        Catch ex As Exception

        End Try

        fbx_matrix.SetTRS(lt, lr, ds)
        fbx_matrix = gm
        fbx_matrix.Transpose()

        build_fbx_matrix(i, fbx_matrix)

        '---------------------------------------------------------------
        Dim mat_cnt As Integer = mesh.Node.GetSrcObjectCount(FbxSurfaceMaterial.ClassId)
        Dim material As FbxSurfaceMaterial = mesh.Node.GetSrcObject(FbxSurfaceMaterial.ClassId, 0)
        Dim property_ As FbxProperty = Nothing
        Dim texture As FbxTexture
        'we never read a Ambient texture. Only Diffuse and Bump....
        Dim uv_scaling, uv_offset As New FbxVector2
        fbxgrp(i).texture_count = 1
        Try
            'diffuse texture.. color_name
            property_ = material.FindProperty(FbxSurfaceMaterial.SDiffuse)
            If property_ IsNot Nothing Then

                texture = property_.GetSrcObject(FbxTexture.ClassId, 0)
                If texture IsNot Nothing Then

                    uv_offset.X = texture.TranslationU
                    uv_offset.Y = texture.TranslationV
                    uv_scaling.X = texture.ScaleU
                    uv_scaling.Y = texture.ScaleV

                    fbxgrp(i).color_name = texture.Name
                    If fbxgrp(i).color_name.Contains("base") Then
                        fbxgrp(i).color_name = texture.FileName
                    End If
                    fbxgrp(i).color_Id = -1
                    frmMain.info_Label.Text = "Loading Texture: " + Path.GetFileName(fbxgrp(i).color_name)
                    Application.DoEvents()
                    fbxgrp(i).color_Id = get_fbx_texture(fbxgrp(i).color_name)
                Else
                    fbxgrp(i).color_Id = white_id
                End If
            Else
                fbxgrp(i).color_Id = white_id

            End If
            If fbxgrp(i).color_Id = 0 Then
                fbxgrp(i).color_Id = white_id
            End If
        Catch ex As Exception
            fbxgrp(i).color_Id = white_id
        End Try

        Try
            'normal map... normal_name
            property_ = material.FindProperty(FbxSurfaceMaterial.SBump)
            If property_ IsNot Nothing Then
                texture = property_.GetSrcObject(FbxTexture.ClassId, 0)
                If texture IsNot Nothing Then
                    fbxgrp(i).normal_name = texture.Name
                    If fbxgrp(i).normal_name.Contains("base") Then
                        fbxgrp(i).normal_name = texture.FileName
                    End If

                    frmMain.info_Label.Text = "Loading Texture: " + fbxgrp(i).normal_name
                    Application.DoEvents()
                    fbxgrp(i).normal_Id = -1
                    fbxgrp(i).normal_Id = get_fbx_texture(fbxgrp(i).normal_name)
                    fbxgrp(i).bumped = True
                    fbxgrp(i).texture_count = 2
                Else
                    property_ = material.FindProperty(FbxSurfaceMaterial.SNormalMap)
                    texture = property_.GetSrcObject(FbxTexture.ClassId, 0)
                    If texture IsNot Nothing Then

                        fbxgrp(i).normal_name = texture.Name
                        If fbxgrp(i).normal_name.Contains("normal") Then
                            fbxgrp(i).normal_name = texture.FileName
                        End If
                        frmMain.info_Label.Text = "Loading Texture: " + fbxgrp(i).normal_name
                        Application.DoEvents()
                        fbxgrp(i).normal_Id = -1
                        fbxgrp(i).normal_Id = get_fbx_texture(fbxgrp(i).normal_name)
                        fbxgrp(i).bumped = True
                        fbxgrp(i).texture_count = 2

                    Else
                        Dim texture_n = fbxgrp(i).color_name.Replace("AM", "ANM")
                        If File.Exists(texture_n) Then
                            fbxgrp(i).normal_name = texture_n
                            frmMain.info_Label.Text = "Loading Texture: " + texture_n
                            Application.DoEvents()
                            fbxgrp(i).normal_Id = -1
                            fbxgrp(i).normal_Id = get_fbx_texture(texture_n)
                            fbxgrp(i).bumped = True
                            fbxgrp(i).texture_count = 2
                        Else
                            fbxgrp(i).bumped = False
                            fbxgrp(i).texture_count = 1
                        End If

                    End If
                End If
            End If
        Catch ex As Exception
            fbxgrp(i).normal_Id = white_id

        End Try

        ' WG has made all simple shaders useless in the tanks visual files.
        ' All shaders must now be PBS_Tank or PBS_Tank_Skinned
        ' There is no use for specular textures in PBS shaders.
        'Try
        '    'specular map... specular_name
        '    property_ = material.FindProperty(FbxSurfaceMaterial.SSpecularFactor)
        '    texture = property_.GetSrcObject(FbxTexture.ClassId, 0)
        '    If texture IsNot Nothing Then
        '        Dim fp = Path.GetDirectoryName(frmMain.OpenFileDialog1.FileName) + "\" + texture.RelativeFileName
        '        fbxgrp(i).specular_name = fix_texture_path(fp)
        '        frmMain.info_Label.Text = "Loading Texture: " + fbxgrp(i).specular_name
        '        Application.DoEvents()
        '        fbxgrp(i).specular_id = -1
        '        fbxgrp(i).specular_id = get_fbx_texture(fbxgrp(i).specular_name)
        '        fbxgrp(i).texture_count = 3
        '        If fbxgrp(i).specular_id = 0 Then
        '            fbxgrp(i).specular_id = white_id
        '        End If
        '    End If
        'Catch ex As Exception

        'End Try
        Return get_mesh_geo(i, childnode, start_vertex, start_index, scene, rootnode, mesh)
    End Function
    Private Function readMeshdata_primitives(ByVal i As Integer, ByRef childnode As FbxNode,
                                start_vertex As Integer, start_index As Integer,
                                scene As FbxScene, rootnode As FbxNode, mesh As FbxMesh)

        ReDim Preserve fbxgrp(i)


        fbxgrp(i).name = childnode.NameOnly
        'get transform information -------------------------------------
        Dim fbx_matrix As New FbxXMatrix
        fbxgrp(i).rotation = New FbxVector4
        fbxgrp(i).translation = New FbxVector4
        fbxgrp(i).scale = New FbxVector4
        fbxgrp(i).scale.X = 1.0
        fbxgrp(i).scale.Y = 1.0
        fbxgrp(i).scale.Z = 1.0

        Dim t As New FbxTime
        Dim GlobalUnitScale = scene.GlobalSettings.FindProperty("UnitScaleFactor", False).GetValueAsDouble

        Dim ls = childnode.GetLocalSFromDefaultTake(FbxNode.PivotSet.SourceSet)
        If ls.X = 1.0 Then
            ls.X = 0.1
            ls.Y = 0.1
            ls.Z = 1.0
        End If

        Dim nodeGT = rootnode.GetGlobalFromDefaultTake(FbxNode.PivotSet.DestinationSet)

        Dim lr = childnode.GetLocalRFromDefaultTake(FbxNode.PivotSet.SourceSet)
        Dim lt = childnode.GetLocalTFromCurrentTake(t)
        Dim gr = childnode.Parent.GetLocalRFromCurrentTake(t)

        Dim scaling = childnode.Scaling.GetValueAsDouble3

        fbxgrp(i).rotation = childnode.GetGeometricRotation(FbxNode.PivotSet.SourceSet)
        fbxgrp(i).translation = childnode.GetGeometricTranslation(FbxNode.PivotSet.SourceSet)
        fbxgrp(i).scale = childnode.GetGeometricScaling(FbxNode.PivotSet.SourceSet)
        fbx_matrix.SetIdentity()

        Dim dr As New FbxVector4
        Dim dt As New FbxVector4
        Dim ds As New FbxVector4

        Dim gm = childnode.GetGlobalFromCurrentTake(t)

        childnode.GetDefaultR(dr)
        childnode.GetDefaultS(ds)
        childnode.GetDefaultT(dt)
        fbxgrp(i).rotation = childnode.GetGeometricRotation(FbxNode.PivotSet.SourceSet)
        fbxgrp(i).translation = childnode.GetGeometricTranslation(FbxNode.PivotSet.SourceSet)
        fbxgrp(i).scale = childnode.GetGeometricScaling(FbxNode.PivotSet.SourceSet)
        Dim TnR As Double = 0
        Try
            TnR = Round(fbxgrp(i).rotation.X, 6) + Round(fbxgrp(i).rotation.Y, 6) + Round(fbxgrp(i).rotation.Z, 6) _
                + Round(fbxgrp(i).translation.X, 6) + Round(fbxgrp(i).translation.Y, 6) + Round(fbxgrp(i).translation.Z, 6)
        Catch ex As Exception

        End Try

        fbx_matrix.SetTRS(lt, lr, ds)
        fbx_matrix = gm
        fbx_matrix.Transpose()

        build_fbx_matrix(i, fbx_matrix)
        '---------------------------------------------------------------
        Dim mat_cnt As Integer = mesh.Node.GetSrcObjectCount(FbxSurfaceMaterial.ClassId)
        Dim material As FbxSurfaceMaterial = mesh.Node.GetSrcObject(FbxSurfaceMaterial.ClassId, 0)
        Return get_mesh_geo(i, childnode, start_vertex, start_index, scene, rootnode, mesh)
    End Function
    Private Function get_mesh_geo(ByVal fbx_idx As Integer, ByRef childnode As FbxNode,
                                    start_vertex As Integer, start_index As Integer,
                                    scene As FbxScene, rootnode As FbxNode, mesh As FbxMesh)

        Dim uvlayer1 As FbxLayerElementUV = mesh.GetLayer(0).GetUVs
        Dim property_ As FbxProperty = Nothing
        Dim texture As FbxTexture
        Dim material As FbxSurfaceMaterial = mesh.Node.GetSrcObject(FbxSurfaceMaterial.ClassId, 0)
        Dim nVertices = mesh.Normals.Count
        '###############################################
        Dim index_mode = uvlayer1.Reference_Mode

        Dim eNormals As FbxLayerElementNormal = mesh.GetLayer(0).Normals
        Dim uv2_Layer As FbxLayerElementUV = Nothing
        If mesh.UVLayerCount = 2 Then
            property_ = material.FindProperty(FbxSurfaceMaterial.SSpecularFactor)
            texture = property_.GetSrcObject(FbxTexture.ClassId, 0)
            If texture Is Nothing Then
                uv2_Layer = mesh.GetLayer(1).GetUVs
                'Stop
            End If
        End If
        Dim cp_cnt As UInt32 = mesh.ControlPoints.Length
        Dim polycnt = mesh.PolygonCount
        Dim uvCount As UInt32 = uvlayer1.IndexArray.Count / 3
        fbxgrp(fbx_idx).nPrimitives_ = polycnt
        fbxgrp(fbx_idx).nVertices_ = nVertices
        fbxgrp(fbx_idx).startIndex_ = start_index : start_index += polycnt * 3
        fbxgrp(fbx_idx).startVertex_ = start_vertex : start_vertex += nVertices * 40

        ReDim fbxgrp(fbx_idx).cPoints(cp_cnt)
        mesh.ControlPoints.CopyTo(fbxgrp(fbx_idx).cPoints, 0)
        ReDim Preserve fbxgrp(fbx_idx).vertices(polycnt * 3)
        ReDim Preserve fbxgrp(fbx_idx).indices(polycnt * 3)
        Dim vertexId As Integer = 0
        For k = 0 To polycnt * 3 - 1
            fbxgrp(fbx_idx).vertices(k) = New vertice_
            fbxgrp(fbx_idx).indices(k) = New uvect3
        Next

        Dim colorLayer1 As FbxLayerElementVertexColor = mesh.GetLayer(0).VertexColors
        Dim normal_layer = mesh.GetLayer(0).Normals
        Dim uv_layer = mesh.GetLayer(0).DiffuseUV


        Dim max_cp_index As Integer
        For i = 0 To polycnt - 1
            Dim pv_cnt As Integer = mesh.GetPolygonSize(0)
            If pv_cnt < 3 Or pv_cnt > 3 Then
                MsgBox("Your mesh is not made of triangles! ID:" + fbxgrp(fbx_idx).name, MsgBoxStyle.Exclamation, "FBX Mesh Problem")
                Return False
            End If

            For j = 0 To 2

                '===============================================================================
                'position
                Dim cp_index As Integer = Math.Abs(mesh.GetPolygonVertex(i, j))
                If cp_index > max_cp_index Then max_cp_index = cp_index
                'Debug.WriteLine(vertexId.ToString + " " + cp_index.ToString)
                Dim vertex As FbxVector4 = fbxgrp(fbx_idx).cPoints(cp_index)
                fbxgrp(fbx_idx).indices(vertexId).v1 = vertexId
                '===============================================================================
                'normals
                Dim normal As New FbxVector4
                If normal_layer.Mapping_Mode = FbxLayerElement.MappingMode.ByPolygonVertex Then
                    Select Case normal_layer.Reference_Mode

                        Case FbxLayerElement.ReferenceMode.Direct
                            normal = normal_layer.DirectArray.GetAt(vertexId)
                            Exit Select
                        Case FbxLayerElement.ReferenceMode.IndexToDirect
                            Dim n_id = normal_layer.IndexArray.GetAt(vertexId)
                            normal = normal_layer.DirectArray.GetAt(n_id)

                    End Select

                ElseIf normal_layer.Mapping_Mode = FbxLayerElement.MappingMode.ByControlPoint Then
                    Select Case normal_layer.Reference_Mode
                        Case FbxLayerElement.ReferenceMode.Direct
                            normal = normal_layer.DirectArray.GetAt(cp_index)
                            Exit Select
                        Case FbxLayerElement.ReferenceMode.IndexToDirect
                            Dim n_id = normal_layer.IndexArray.GetAt(vertexId)
                            normal = normal_layer.DirectArray.GetAt(n_id)
                    End Select

                End If
                '===============================================================================
                'UVs
                Dim uv As New FbxVector2
                If uv_layer Is Nothing Then
                    MsgBox("No Uvs for mesh! ID:" + fbxgrp(fbx_idx).name, MsgBoxStyle.Exclamation, "FBX Mesh Problem!")
                    Return False
                End If
                Select Case uv_layer.Mapping_Mode
                    Case FbxLayerElement.MappingMode.ByControlPoint
                        Select Case uv_layer.Reference_Mode
                            Case FbxLayerElement.ReferenceMode.Direct
                                uv = uv_layer.DirectArray.GetAt(cp_index)
                                Exit Select
                            Case FbxLayerElement.ReferenceMode.IndexToDirect
                                Dim n_id = uv_layer.IndexArray.GetAt(cp_index)
                                uv = uv_layer.DirectArray.GetAt(n_id)

                        End Select
                        Exit Select
                    Case FbxLayerElement.MappingMode.ByPolygonVertex
                        Dim uv_index = mesh.GetTextureUVIndex(i, j)
                        Select Case uv_layer.Reference_Mode
                            Case FbxLayerElement.ReferenceMode.Direct
                            Case FbxLayerElement.ReferenceMode.IndexToDirect
                                uv = uv_layer.DirectArray.GetAt(uv_index)
                        End Select
                End Select
                '===============================================================================
                'UVs
                Dim uv2 As New FbxVector2
                If mesh.UVLayerCount = 2 Then

                    If uv2_Layer IsNot Nothing Then
                        fbxgrp(fbx_idx).has_uv2 = 1
                        save_has_uv2 = True
                        Select Case uv2_Layer.Mapping_Mode
                            Case FbxLayerElement.MappingMode.ByControlPoint
                                Select Case uv2_Layer.Reference_Mode
                                    Case FbxLayerElement.ReferenceMode.Direct
                                        uv2 = uv2_Layer.DirectArray.GetAt(cp_index)
                                        Exit Select
                                    Case FbxLayerElement.ReferenceMode.IndexToDirect
                                        Dim n_id = uv2_Layer.IndexArray.GetAt(cp_index)
                                        uv2 = uv2_Layer.DirectArray.GetAt(n_id)

                                End Select
                                Exit Select
                            Case FbxLayerElement.MappingMode.ByPolygonVertex
                                Dim uv2_index = mesh.GetTextureUVIndex(i, j)
                                Select Case uv2_Layer.Reference_Mode
                                    Case FbxLayerElement.ReferenceMode.Direct
                                    Case FbxLayerElement.ReferenceMode.IndexToDirect
                                        uv2 = uv2_Layer.DirectArray.GetAt(uv2_index)
                                End Select

                        End Select
                    End If
                End If
                '===============================================================================
                'vertex color
                Dim color1 As New FbxColor
                If colorLayer1 IsNot Nothing Then
                    fbxgrp(fbx_idx).has_Vcolor = 0

                    Dim cv_refmode = colorLayer1.Reference_Mode
                    If cv_refmode = FbxLayerElement.ReferenceMode.IndexToDirect Then
                        color1 = colorLayer1.DirectArray(colorLayer1.IndexArray.GetAt(vertexId))
                        fbxgrp(fbx_idx).has_Vcolor = 1

                    Else
                        If cv_refmode = FbxLayerElement.ReferenceMode.Direct Then
                            color1 = colorLayer1.DirectArray(vertexId)
                            fbxgrp(fbx_idx).has_Vcolor = 1
                        End If
                    End If
                End If
                fbxgrp(fbx_idx).vertices(vertexId).x = vertex.X
                fbxgrp(fbx_idx).vertices(vertexId).y = vertex.Y
                fbxgrp(fbx_idx).vertices(vertexId).z = vertex.Z
                fbxgrp(fbx_idx).vertices(vertexId).u = uv.X
                fbxgrp(fbx_idx).vertices(vertexId).v = -uv.Y
                fbxgrp(fbx_idx).vertices(vertexId).u2 = uv2.X
                fbxgrp(fbx_idx).vertices(vertexId).v2 = -uv2.Y

                fbxgrp(fbx_idx).vertices(vertexId).nx = normal.X
                fbxgrp(fbx_idx).vertices(vertexId).ny = normal.Y
                fbxgrp(fbx_idx).vertices(vertexId).nz = normal.Z
                fbxgrp(fbx_idx).vertices(vertexId).n = packnormalFBX888(normal)
                fbxgrp(fbx_idx).vertices(vertexId).index_1 = CByte(color1.Red * 255)
                fbxgrp(fbx_idx).vertices(vertexId).index_2 = CByte(color1.Green * 255)
                fbxgrp(fbx_idx).vertices(vertexId).index_3 = CByte(color1.Blue * 255)
                fbxgrp(fbx_idx).vertices(vertexId).index_4 = CByte(color1.Alpha * 255)

                fbxgrp(fbx_idx).vertices(vertexId).r = CByte(color1.Red * 255)
                fbxgrp(fbx_idx).vertices(vertexId).g = CByte(color1.Green * 255)
                fbxgrp(fbx_idx).vertices(vertexId).b = CByte(color1.Blue * 255)
                fbxgrp(fbx_idx).vertices(vertexId).a = CByte(color1.Alpha * 255)


                vertexId += 1
            Next
        Next
        ReDim Preserve fbxgrp(fbx_idx).vertices(vertexId - 1)
        ReDim Preserve fbxgrp(fbx_idx).indices(vertexId - 1)
        fbxgrp(fbx_idx).nVertices_ = max_cp_index + 1

        'check_winding_order(fbx_idx)

        create_TBNS2(fbx_idx)

        Return True
    End Function



    Public Sub process_fbx_data()
        Try

            'we need to reorder the FBX read by its ID tag
            Dim total = fbxgrp.Length
            ReDim t_fbx(total)
            Dim last As Integer = 1
            Dim pnt(30) As Integer
            'move to right locations....
            For i = 1 To fbxgrp.Length - 1
                If fbxgrp(i).name.ToLower.Contains("~") Then

                    Dim n = fbxgrp(i).name
                    Dim a = n.Split("~")
                    Dim idx = Convert.ToInt32(a(2))
                    move_fbx_entry(t_fbx(idx), fbxgrp(i), i, idx)
                    last += 1

                End If
            Next
            'sort tank parts and move any new items to the end.
            For i = 1 To fbxgrp.Length - 1
                If Not fbxgrp(i).name.ToLower.Contains("~") Then
                    move_fbx_entry(t_fbx(last), fbxgrp(i), last, i)
                    last += 1
                End If
            Next
            ' write back the sorted fbx entries.
            For i = 1 To fbxgrp.Length - 1
                move_fbx_entry(fbxgrp(i), t_fbx(i), last, i)
                Dim tn = fbxgrp(i).name.Split("~")
                frmComponentView.add_to_fbx_list(i, Path.GetFileNameWithoutExtension(tn(0)))
                frmReverseVertexWinding.add_to_fbx_list(i, Path.GetFileNameWithoutExtension(tn(0)))
            Next

            ReDim t_fbx(0) ' clean up some memory


            GC.Collect()
        Catch ex As Exception
            MsgBox(ex.Message, MsgBoxStyle.Exclamation, "error")
            Return
        End Try
        Try
            get_component_index() 'build indexing table

        Catch ex As Exception
            MsgBox(ex.Message, MsgBoxStyle.Exclamation, "error")

        End Try
    End Sub
    Private Sub move_fbx_entry(ByRef fbx_in As _grps, ByRef fbx_out As _grps, ByVal i As Integer, ByVal idx As Integer)
        'sorts the tankk parts by id in file name.
        fbx_in = New _grps

        fbx_in.name = fbx_out.name

        fbx_in.color_name = fbx_out.color_name
        fbx_in.normal_name = fbx_out.normal_name
        fbx_in.GMM_name = fbx_out.GMM_name
        fbx_in.ao_name = fbx_out.ao_name

        fbx_in.color_Id = fbx_out.color_Id
        fbx_in.normal_Id = fbx_out.normal_Id
        fbx_in.GMM_Id = fbx_out.GMM_Id
        fbx_in.ao_id = fbx_out.ao_id

        fbx_in.call_list = fbx_out.call_list

        fbx_in.nPrimitives_ = fbx_out.nPrimitives_
        fbx_in.nVertices_ = fbx_out.nVertices_
        fbx_in.startIndex_ = fbx_out.startIndex_
        fbx_in.startVertex_ = fbx_out.startVertex_

        fbx_in.specular_name = fbx_out.specular_name
        fbx_in.specular_id = fbx_out.specular_id

        fbx_in.texture_count = fbx_out.texture_count
        fbx_in.has_uv2 = fbx_out.has_uv2
        fbx_in.has_color = fbx_out.has_color
        fbx_in.bumped = fbx_out.bumped
        fbx_in.stride = fbx_out.stride
        fbx_in.is_new_model = fbx_out.is_new_model

        If fbx_out.vertColor IsNot Nothing Then
            ReDim fbx_in.vertColor(fbx_out.vertColor.Length - 1)
            Array.Copy(fbx_out.vertColor, fbx_in.vertColor, fbx_out.vertColor.Length)
        End If
        If fbx_out.weight0 IsNot Nothing Then
            ReDim fbx_in.weight0(fbx_out.vertColor.Length - 1)
            Array.Copy(fbx_out.weight0, fbx_in.weight0, fbx_out.weight0.Length)
        End If



        ReDim fbx_in.matrix(15)
        For j = 0 To 15
            fbx_in.matrix(j) = fbx_out.matrix(j)
        Next
        ReDim fbx_in.vertices(fbx_out.vertices.Length - 1)
        For j = 0 To fbx_out.vertices.Length - 1
            fbx_in.vertices(j) = New vertice_
            fbx_in.vertices(j).index_1 = fbx_out.vertices(j).index_1
            fbx_in.vertices(j).index_2 = fbx_out.vertices(j).index_2
            fbx_in.vertices(j).index_3 = fbx_out.vertices(j).index_3
            fbx_in.vertices(j).index_4 = fbx_out.vertices(j).index_4

            fbx_in.vertices(j).weight_1 = fbx_out.vertices(j).weight_1
            fbx_in.vertices(j).weight_2 = fbx_out.vertices(j).weight_2
            fbx_in.vertices(j).weight_3 = fbx_out.vertices(j).weight_3
            fbx_in.vertices(j).weight_4 = fbx_out.vertices(j).weight_4

            fbx_in.vertices(j).n = fbx_out.vertices(j).n

            fbx_in.vertices(j).x = fbx_out.vertices(j).x
            fbx_in.vertices(j).y = fbx_out.vertices(j).y
            fbx_in.vertices(j).z = fbx_out.vertices(j).z

            fbx_in.vertices(j).nx = fbx_out.vertices(j).nx
            fbx_in.vertices(j).ny = fbx_out.vertices(j).ny
            fbx_in.vertices(j).nz = fbx_out.vertices(j).nz

            fbx_in.vertices(j).n = fbx_out.vertices(j).n
            fbx_in.vertices(j).t = fbx_out.vertices(j).t
            fbx_in.vertices(j).bn = fbx_out.vertices(j).bn

            fbx_in.vertices(j).u = fbx_out.vertices(j).u
            fbx_in.vertices(j).v = fbx_out.vertices(j).v

            fbx_in.vertices(j).u2 = fbx_out.vertices(j).u2
            fbx_in.vertices(j).v2 = fbx_out.vertices(j).v2

            fbx_in.vertices(j).r = fbx_out.vertices(j).r
            fbx_in.vertices(j).g = fbx_out.vertices(j).g
            fbx_in.vertices(j).b = fbx_out.vertices(j).b
            fbx_in.vertices(j).a = fbx_out.vertices(j).a

            fbx_in.vertices(j).bnx = fbx_out.vertices(j).bnx
            fbx_in.vertices(j).bny = fbx_out.vertices(j).bny
            fbx_in.vertices(j).bnz = fbx_out.vertices(j).bnz

            fbx_in.vertices(j).tx = fbx_out.vertices(j).tx
            fbx_in.vertices(j).ty = fbx_out.vertices(j).ty
            fbx_in.vertices(j).tz = fbx_out.vertices(j).tz

        Next
        ReDim fbx_in.indices(fbx_out.indices.Length - 1)
        For j = 0 To fbx_out.indices.Length - 1
            fbx_in.indices(j) = New uvect3
            fbx_in.indices(j).v1 = fbx_out.indices(j).v1
            fbx_in.indices(j).v2 = fbx_out.indices(j).v2
            fbx_in.indices(j).v3 = fbx_out.indices(j).v3
        Next

    End Sub

    Private Sub get_component_index()
        Dim ct, ht, tt, gt As Integer
        Dim c_cnt, h_cnt, t_cnt, g_cnt As Integer
        Dim odd_model As Boolean
        '---------------------------------------------------------------------------------------------------
        'find out if we have a wrongly named model in the FBX
        odd_model = False
        For i = 1 To fbxgrp.Length - 1
            If Not odd_model Then
                If fbxgrp(i).name.ToLower.Contains("chassis") Or
                    fbxgrp(i).name.ToLower.Contains("hull") Or
                    fbxgrp(i).name.ToLower.Contains("turret") Or
                    fbxgrp(i).name.ToLower.Contains("gun") Then
                Else
                    odd_model = True
                End If
            End If
        Next

        '---------------------------------------------------------------------------------------------------
        'sort out how many are of what type in the fbx
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
        Dim target_group As Integer = 0
        For i = 1 To fbxgrp.Length - 1
            'set some booleans 
            fbxgrp(i).is_carraige = False
            fbxgrp(i).component_visible = True
            'figure out if this is a chasss component and get component counts
            If fbxgrp(i).name.ToLower.Contains("chassis") Then
                target_group = ct
                If fbxgrp(i).name.ToLower.Contains("\crash\") Then
                    CRASH_MODE = True
                End If
                fbxgrp(i).is_carraige = True
                ReDim Preserve m_groups(1).list(ct)
                ReDim Preserve m_groups(1).group_list(ct)
                ReDim Preserve m_groups(1).f_name(ct)
                ReDim Preserve m_groups(1).section_names(ct)
                ReDim Preserve m_groups(1).package_id(ct)
                m_groups(1).list(ct) = i
                m_groups(1).cnt = ct + 1
                m_groups(1).m_type = 1
                ar = fbxgrp(i).name.Split("~")
                m_groups(1).f_name(ct) = "vehicles\" + ar(0)
                If ar.Length > 1 Then
                    m_groups(1).group_list(ct) = ar(2)
                    m_groups(1).section_names(ct) = ar(0)
                Else
                    m_groups(1).section_names(ct) = ar(0)
                End If
                ct += 1
            End If
            If fbxgrp(i).name.ToLower.Contains("hull") Then
                target_group = ht
                ReDim Preserve m_groups(2).list(ht)
                ReDim Preserve m_groups(2).f_name(ht)
                ReDim Preserve m_groups(2).package_id(ht)
                m_groups(2).cnt = ht + 1
                m_groups(2).list(ht) = i
                m_groups(2).m_type = 2
                ar = fbxgrp(i).name.Split("~")
                m_groups(2).f_name(ht) = "vehicles\" + ar(0)

                ht += 1
            End If
            If fbxgrp(i).name.ToLower.Contains("turret") Then
                target_group = tt
                ReDim Preserve m_groups(3).list(tt)
                ReDim Preserve m_groups(3).f_name(tt)
                ReDim Preserve m_groups(3).package_id(tt)
                m_groups(3).cnt = tt + 1
                m_groups(3).list(tt) = i
                m_groups(3).m_type = 3
                ar = fbxgrp(i).name.Split("~")
                m_groups(3).f_name(tt) = "vehicles\" + ar(0)

                tt += 1
            End If
            If fbxgrp(i).name.ToLower.Contains("gun") Then
                target_group = gt
                ReDim Preserve m_groups(4).list(gt)
                ReDim Preserve m_groups(4).f_name(gt)
                ReDim Preserve m_groups(4).package_id(gt)
                m_groups(4).cnt = gt + 1
                m_groups(4).list(gt) = i
                m_groups(4).m_type = 4
                ar = fbxgrp(i).name.Split("~")
                m_groups(4).f_name(gt) = "vehicles\" + ar(0)

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
                If m_groups(i).f_name(j).Contains("\l\") Then
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

            Dim fp = Path.GetDirectoryName(file_name)
            Dim fnm = Path.GetFileName(file_name)

            Select Case True
                Case fnm.ToLower.Contains("chass")
                    fnm = "Chassis.model"
                    Exit Select
                Case fnm.ToLower.Contains("hull")
                    fnm = "Hull.model"
                    Exit Select
                Case fnm.ToLower.Contains("turr")
                    Dim arr = fnm.Split(".")
                    fnm = arr(0) + ".model"
                    Exit Select
                Case fnm.ToLower.Contains("gun")
                    Dim arr = fnm.Split(".")
                    fnm = arr(0) + ".model"
                    'fnm = "chassis.model_processed"
                    Exit Select

            End Select
            file_name = fp + "\" + fnm
            file_name = file_name.Replace("\l\", "\lod0\")
            Dim ta = file_name.Split("\normal")
            TANK_NAME = ta(0)
            frmMain.Text = "File: " + ta(0)
            LOADING_FBX = True


            Dim success As Boolean = False
            If file_name.ToLower.Contains("chassis") Then
                success = build_primitive_data(False)
            Else
                success = build_primitive_data(True)
            End If
            If Not success Then
                MsgBox("Fail loading original model", MsgBoxStyle.Exclamation, "Load failure!")
                remove_loaded_fbx()
                frmMain.clean_house()
            End If
        Next
        For i = 1 To 4
            For cnt = 0 To m_groups(i).cnt - 1

                Dim tname = m_groups(i).f_name(cnt)
                Dim fp = Path.GetDirectoryName(file_name)
                Dim fnm = Path.GetFileName(tname)

                Select Case True
                    Case fnm.ToLower.Contains("chass")
                        fnm = "Chassis.model"
                        tname = fp + "\" + fnm
                        tname = tname.Replace("\l\", "\lod0\")
                        file_name = tname
                        m_groups(i).f_name(cnt) = file_name
                        Exit Select
                    Case fnm.ToLower.Contains("hull")
                        fnm = "Hull.model"
                        tname = fp + "\" + fnm
                        tname = tname.Replace("\l\", "\lod0\")
                        file_name = tname
                        m_groups(i).f_name(cnt) = file_name
                        Exit Select
                    Case fnm.ToLower.Contains("turr")
                        Dim arr = fnm.Split(".")
                        fnm = arr(0) + ".model"
                        tname = fp + "\" + fnm
                        tname = tname.Replace("\l\", "\lod0\")
                        file_name = tname
                        m_groups(i).f_name(cnt) = file_name
                        Exit Select
                    Case fnm.ToLower.Contains("gun")
                        Dim arr = fnm.Split(".")
                        fnm = arr(0) + ".model"
                        tname = fp + "\" + fnm
                        tname = tname.Replace("\l\", "\lod0\")
                        file_name = tname
                        m_groups(i).f_name(cnt) = file_name
                        'fnm = "chassis.model_processed"
                        Exit Select

                End Select
                If fbxgrp(m_groups(i).list(cnt)).is_new_model Then
                    m_groups(i).f_name(cnt) = fnm
                End If
            Next
            '---------------------------------------------------------------------------------------------------
            'get the xml for this tank.
            file_name = m_groups(i).f_name(0).Replace("\", "/")
            ar = file_name.Split("/")
            Try
                Dim xml_file = ar(0) + "\" + ar(1) + "\" + ar(2) + ".xml"
                frmMain.get_tank_parts_from_xml(xml_file, New DataSet)
                frmMain.Text = "File: " + ar(0) + "\" + ar(1) + "\" + ar(2) ' title on main window
            Catch ex As Exception
                'MsgBox("" + vbCrLf + "" + vbCrLf + "", MsgBoxStyle.Exclamation, "error..")
                'Return
            End Try
        Next

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
        Dim t_fbx, t_mdl As Integer
        t_fbx = ct + ht + tt + gt
        t_mdl = c_cnt + h_cnt + t_cnt + g_cnt
        'if t_fbx = t_mdl than we have the same componet counts.
        'Check of one of them has been modified.
        Dim CB, HB, TB, GB As Boolean
        Dim c_new, h_new, t_new, g_new As Boolean
        CB = False : HB = False : TB = False : GB = False ' these default to false but set them anyway
        c_new = False : h_new = False : t_new = False : g_new = False
        Dim fn As String = ""
        If t_fbx <> t_mdl Then
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


        End If
        For i = 1 To fbxgrp.Length - 1
            If Not fbxgrp(i).is_new_model Then
                fbxgrp(i).is_GAmap = 1 ' not PBS
                fbxgrp(i).alphaTest = 0
            Else
                fbxgrp(i).is_GAmap = 1 'is PBS
                fbxgrp(i).alphaTest = 1
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
        Try
            fn = ar(0) + "\" + ar(1) + "\" + ar(2)
        Catch ex As Exception

        End Try
        current_tank_name = fn ' Path.GetDirectoryName(file_name)
        Dim dp = My.Settings.res_mods_path + "\" + fn
        frmWritePrimitive.SAVE_NAME = dp
        If Not Directory.Exists(dp) Then
            If MsgBox("It appears You have not extracted data for this model." + vbCrLf +
                      "There is no place to save this new Model." + vbCrLf +
                       "Would you like to extract the data from the .PKG files?", MsgBoxStyle.YesNo, "Extract?") = MsgBoxResult.Yes Then
                file_name = "1:dummy:" + Path.GetFileNameWithoutExtension(dp.Replace("/", "\"))
                frmMain.find_icon_image(TANK_NAME)
                frmMain.m_extract.PerformClick()
            End If

        End If
        'set which group has new models or changed data
        frmWritePrimitive.Opacity = 0

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
        'fbx pick list
        For i = 1 To fbxgrp.Length - 1
            create_TBNS(i)
        Next
        For i = 1 To fbxgrp.Length - 1
            fbxgrp(i).visible = True
            Dim cpl = Gl.glGenLists(1)
            fbxgrp(i).vertex_pick_list = cpl
            Gl.glNewList(cpl, Gl.GL_COMPILE)
            a = i + 10
            Gl.glBegin(Gl.GL_TRIANGLES)
            For k As UInt32 = 0 To fbxgrp(i).nPrimitives_ - 1
                Dim p1 = fbxgrp(i).indices(k).v1
                Dim p2 = fbxgrp(i).indices(k).v2
                Dim p3 = fbxgrp(i).indices(k).v3
                Dim v1 = fbxgrp(i).vertices(p1)
                Dim v2 = fbxgrp(i).vertices(p2)
                Dim v3 = fbxgrp(i).vertices(p3)
                Dim t = CInt(k + 1)
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
        'model pick list
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
        frmWritePrimitive.Opacity = 100

        Application.DoEvents()
        MODEL_LOADED = True
        frmMain.m_hide_show_components.Enabled = True
        frmMain.m_set_vertex_winding_order.Enabled = True
    End Sub

    Public Sub make_fbx_display_lists(ByVal cnt As Integer, ByVal jj As Integer)
        Gl.glBegin(Gl.GL_TRIANGLES)
        For z As UInt32 = 0 To (cnt) - 1
            make_triangle(jj, fbxgrp(jj).indices(z).v1)
            make_triangle(jj, fbxgrp(jj).indices(z).v2)
            make_triangle(jj, fbxgrp(jj).indices(z).v3)
        Next
        Gl.glEnd()
    End Sub

    Private Sub make_triangle(ByVal jj As Integer, ByVal i As Integer)
        Gl.glNormal3f(fbxgrp(jj).vertices(i).nx, fbxgrp(jj).vertices(i).ny, fbxgrp(jj).vertices(i).nz)
        Gl.glMultiTexCoord2f(Gl.GL_TEXTURE0, -fbxgrp(jj).vertices(i).u, fbxgrp(jj).vertices(i).v)
        Gl.glMultiTexCoord3f(Gl.GL_TEXTURE1, fbxgrp(jj).vertices(i).tx, fbxgrp(jj).vertices(i).ty, fbxgrp(jj).vertices(i).tz)
        Gl.glMultiTexCoord3f(Gl.GL_TEXTURE2, fbxgrp(jj).vertices(i).bnx, fbxgrp(jj).vertices(i).bny, fbxgrp(jj).vertices(i).bnz)
        If fbxgrp(jj).has_color Then
            Gl.glMultiTexCoord3f(Gl.GL_TEXTURE3, CSng(fbxgrp(jj).vertices(i).index_1 / 255.0),
                                                 CSng(fbxgrp(jj).vertices(i).index_2 / 255.0),
                                                 CSng(fbxgrp(jj).vertices(i).index_3 / 255.0))
        Else
            Gl.glMultiTexCoord3f(Gl.GL_TEXTURE3, 0.0!, 0.00!, 0.00!)
        End If

        Gl.glTexCoord2f(-fbxgrp(jj).vertices(i).u, fbxgrp(jj).vertices(i).v)

        Gl.glVertex3f(fbxgrp(jj).vertices(i).x, fbxgrp(jj).vertices(i).y, fbxgrp(jj).vertices(i).z)

    End Sub

    Public Sub flip_winding_group(item As Integer)
        ' Loop through the indices buffer
        For k As Integer = 0 To _group(item).indices.Length - 1
            ' Get the indices for p1, p2, p3
            Dim p1 = _group(item).indices(k).v1
            _group(item).indices(k).v1 = _group(item).indices(k).v2
            _group(item).indices(k).v2 = p1
        Next
        For k As Integer = 0 To _group(item).vertices.Length - 2
            _group(item).vertices(k).nx *= -1
            _group(item).vertices(k).tx *= -1
            _group(item).vertices(k).bnx *= -1
        Next
    End Sub
    Public Sub fix_winding_order(ByVal i As Integer)
        Dim flag As Boolean = False

        ' Loop through the indices buffer
        For k As Integer = 0 To fbxgrp(i).indices.Length - 1
            ' Get the indices for p1, p2, p3
            Dim p1 = fbxgrp(i).indices(k).v1
            Dim p2 = fbxgrp(i).indices(k).v2
            Dim p3 = fbxgrp(i).indices(k).v3

            ' Get the vertices for v1, v2, v3
            Dim v1, v2, v3 As SharpDX.Vector3
            v1.X = fbxgrp(i).vertices(p1).x
            v1.Y = fbxgrp(i).vertices(p1).y
            v1.Z = fbxgrp(i).vertices(p1).z

            v2.X = fbxgrp(i).vertices(p2).x
            v2.Y = fbxgrp(i).vertices(p2).y
            v2.Z = fbxgrp(i).vertices(p2).z

            v3.X = fbxgrp(i).vertices(p3).x
            v3.Y = fbxgrp(i).vertices(p3).y
            v3.Z = fbxgrp(i).vertices(p3).z

            ' Get the normals for v1, v2, and v3
            Dim n1, n2, n3 As SharpDX.Vector3
            n1.X = fbxgrp(i).vertices(p1).nx
            n1.Y = fbxgrp(i).vertices(p1).ny
            n1.Z = fbxgrp(i).vertices(p1).nz

            n2.X = fbxgrp(i).vertices(p2).nx
            n2.Y = fbxgrp(i).vertices(p2).ny
            n2.Z = fbxgrp(i).vertices(p2).nz

            n3.X = fbxgrp(i).vertices(p3).nx
            n3.Y = fbxgrp(i).vertices(p3).ny
            n3.Z = fbxgrp(i).vertices(p3).nz

            ' Calculate the average normal using vertex normals
            Dim avgNormal As SharpDX.Vector3 = (n1 + n2 + n3) / 3

            ' Calculate the geometric normal using the cross product of the triangle's edges
            Dim edge1 As SharpDX.Vector3 = v2 - v1
            Dim edge2 As SharpDX.Vector3 = v3 - v1
            Dim geometricNormal As SharpDX.Vector3 = SharpDX.Vector3.Cross(edge1, edge2)

            ' Normalize both normals to compare them
            avgNormal.Normalize()
            geometricNormal.Normalize()

            ' Compare the normals using the dot product
            Dim dotProduct As Single = SharpDX.Vector3.Dot(avgNormal, geometricNormal)

            ' If the dot product is negative, the winding order is incorrect (clockwise)
            If dotProduct < 0 Then
                flag = True      ' Apply winding order flip
                fbxgrp(i).indices(k).v2 = p3
                fbxgrp(i).indices(k).v3 = p2
            End If
        Next
        If flag Then
            Debug.WriteLine("reversed winding id " + i.ToString)
        End If
    End Sub
    Public Sub fix_winding_order_group(ByVal i As Integer)
        'Return
        ' Loop through the indices buffer
        Dim c As Integer = 0
        For k As Integer = 1 To _group(i).indices.Length - 1
            ' Get the indices for p1, p2, p3
            Dim p1 = _group(i).indices(k).v1
            Dim p2 = _group(i).indices(k).v2
            Dim p3 = _group(i).indices(k).v3

            ' Get the vertices for v1, v2, v3
            Dim v1, v2, v3 As SharpDX.Vector3
            v1.X = _group(i).vertices(p1).x
            v1.Y = _group(i).vertices(p1).y
            v1.Z = _group(i).vertices(p1).z

            v2.X = _group(i).vertices(p2).x
            v2.Y = _group(i).vertices(p2).y
            v2.Z = _group(i).vertices(p2).z

            v3.X = _group(i).vertices(p3).x
            v3.Y = _group(i).vertices(p3).y
            v3.Z = _group(i).vertices(p3).z

            ' Get the normals for v1, v2, and v3
            Dim n1, n2, n3 As SharpDX.Vector3
            n1.X = _group(i).vertices(p1).nx
            n1.Y = _group(i).vertices(p1).ny
            n1.Z = _group(i).vertices(p1).nz

            n2.X = _group(i).vertices(p2).nx
            n2.Y = _group(i).vertices(p2).ny
            n2.Z = _group(i).vertices(p2).nz

            n3.X = _group(i).vertices(p3).nx
            n3.Y = _group(i).vertices(p3).ny
            n3.Z = _group(i).vertices(p3).nz

            ' Calculate the average normal using vertex normals
            Dim avgNormal As SharpDX.Vector3 = (n1 + n2 + n3) / 3

            ' Calculate the geometric normal using the cross product of the triangle's edges
            Dim edge1 As SharpDX.Vector3 = v2 - v1
            Dim edge2 As SharpDX.Vector3 = v3 - v1
            Dim geometricNormal As SharpDX.Vector3 = SharpDX.Vector3.Cross(edge1, edge2)

            ' Normalize both normals to compare them
            avgNormal.Normalize()
            geometricNormal.Normalize()

            ' Compare the normals using the dot product
            Dim dotProduct As Single = SharpDX.Vector3.Dot(avgNormal, geometricNormal)

            ' If the dot product is negative, the winding order is incorrect (clockwise)
            If dotProduct < 0 Then
                ' Apply winding order flip
                _group(i).indices(k).v2 = p3
                _group(i).indices(k).v3 = p2
                c += 1
            End If
        Next
        Debug.WriteLine("fliped _group winding count: " + c.ToString)

    End Sub

    Public Sub check_normal(ByVal i As Integer)
        Dim flip As Single = 1.0!
        If fbxgrp(i).name.ToLower.Contains("turret") Or fbxgrp(i).name.ToLower.Contains("hull") Then
            If Not fbxgrp(i).name.Contains("~") Then
                flip = -1.0!
            End If
        End If

        For p1 As Integer = 0 To fbxgrp(i).nVertices_ - 1
            fbxgrp(i).vertices(p1).nx *= flip
            fbxgrp(i).vertices(p1).ny *= flip
            fbxgrp(i).vertices(p1).nz *= flip
        Next
    End Sub

    Public Sub check_normal_group(ByVal i As Integer)
        If _group(i).color_name Is Nothing Then
            Return
        End If
        Dim flip As Single = 1.0!
        If _group(i).name.ToLower.Contains("turret") Or _group(i).name.ToLower.Contains("hull") Then
            If Not _group(i).name.Contains("!") Then
                flip = 1.0
            Else
                flip = 1.0

            End If
        End If

        For p1 As Integer = 0 To _group(i).nVertices_ - 1
            _group(i).vertices(p1).nx *= flip
        Next
    End Sub


    Private Sub setFbxMatrix(ByRef m_() As Double, ByRef fb As FbxXMatrix)
        Dim m As New SharpDX.Matrix
        For i = 1 To 3
            For j = 1 To 3
                m.Item(j, i) = m_((i * 4) + j)
            Next
        Next
        Dim r As SharpDX.Quaternion
        Dim t, s As SharpDX.Vector3
        m.Decompose(s, r, t)
        Dim vs As New FbxVector4(s.X, s.Y, s.Z, 1.0)
        Dim vt As New FbxVector4(t.X, t.X, t.Z, 1.0)
        Dim vr As New FbxQuaternion(r.X, r.Y, r.Z, 0.0)
        fb.SetTQS(vt, vr, vs)

    End Sub


#Region "TBN Creation functions"

    Public Sub create_TBNS(ByVal id As UInt32)

        Dim cnt = fbxgrp(id).nPrimitives_
        Dim p1, p2, p3 As UInt32
        For i As UInt32 = 0 To cnt - 1
            p1 = fbxgrp(id).indices(i).v1
            p2 = fbxgrp(id).indices(i).v2
            p3 = fbxgrp(id).indices(i).v3
            Dim tan, bn As vect3
            Dim v1, v2, v3 As vect3
            Dim u1, u2, u3, n As vect3
            v1.x = -fbxgrp(id).vertices(p1).x
            v1.y = fbxgrp(id).vertices(p1).y
            v1.z = fbxgrp(id).vertices(p1).z

            v2.x = -fbxgrp(id).vertices(p2).x
            v2.y = fbxgrp(id).vertices(p2).y
            v2.z = fbxgrp(id).vertices(p2).z

            v3.x = -fbxgrp(id).vertices(p3).x
            v3.y = fbxgrp(id).vertices(p3).y
            v3.z = fbxgrp(id).vertices(p3).z
            '
            u1.x = fbxgrp(id).vertices(p1).u
            u1.y = fbxgrp(id).vertices(p1).v

            u2.x = fbxgrp(id).vertices(p2).u
            u2.y = fbxgrp(id).vertices(p2).v

            u3.x = fbxgrp(id).vertices(p3).u
            u3.y = fbxgrp(id).vertices(p3).v

            n.x = fbxgrp(id).vertices(p3).x
            n.y = fbxgrp(id).vertices(p3).y
            n.z = fbxgrp(id).vertices(p3).z

            ComputeTangentBasis(v1, v2, v3, u1, u2, u3, tan, bn) ' calculate tan and biTan
            'tan = OrthogonalizeTangent(tan, n)
            'bn = ComputeBinormal(n, tan)

            save_tbn(id, tan, bn, p1) ' puts xyz values in vertex
            save_tbn(id, tan, bn, p2)
            save_tbn(id, tan, bn, p3)
        Next
        Return
    End Sub
    Public Sub create_TBNS2(ByVal id As UInt32)

        Dim cnt As UInt32 = fbxgrp(id).nPrimitives_
        Dim p1, p2, p3 As UInt32
        ' Iterate through each triangle (assuming primitives are triangles)
        For i As UInt32 = 0 To cnt - 1
            p1 = fbxgrp(id).indices(i).v1
            p2 = fbxgrp(id).indices(i).v2
            p3 = fbxgrp(id).indices(i).v3
            Dim tan, bn As vect3
            Dim v1, v2, v3 As vect3
            Dim u1, u2, u3, n As vect3
            v1.x = -fbxgrp(id).vertices(p1).x
            v1.y = fbxgrp(id).vertices(p1).y
            v1.z = fbxgrp(id).vertices(p1).z

            v2.x = -fbxgrp(id).vertices(p2).x
            v2.y = fbxgrp(id).vertices(p2).y
            v2.z = fbxgrp(id).vertices(p2).z

            v3.x = -fbxgrp(id).vertices(p3).x
            v3.y = fbxgrp(id).vertices(p3).y
            v3.z = fbxgrp(id).vertices(p3).z
            '
            u1.x = fbxgrp(id).vertices(p1).u
            u1.y = fbxgrp(id).vertices(p1).v

            u2.x = fbxgrp(id).vertices(p2).u
            u2.y = fbxgrp(id).vertices(p2).v

            u3.x = fbxgrp(id).vertices(p3).u
            u3.y = fbxgrp(id).vertices(p3).v

            n.x = fbxgrp(id).vertices(p3).x
            n.y = fbxgrp(id).vertices(p3).y
            n.z = fbxgrp(id).vertices(p3).z

            ComputeTangentBasis(v1, v2, v3, u1, u2, u3, tan, bn) ' calculate tan and biTan
            'tan = OrthogonalizeTangent(tan, n)
            'bn = ComputeBinormal(n, tan)

            save_tbn(id, tan, bn, p1) ' puts xyz values in vertex
            save_tbn(id, tan, bn, p2)
            save_tbn(id, tan, bn, p3)
        Next

        ' Exit the subroutine
        Return
    End Sub

    Private Sub save_tbn(id As Integer, tan As vect3, bn As vect3, i As Integer)
        tan = normalize(tan)
        bn = normalize(bn)
        fbxgrp(id).vertices(i).tx = tan.x
        fbxgrp(id).vertices(i).ty = tan.y
        fbxgrp(id).vertices(i).tz = tan.z
        fbxgrp(id).vertices(i).bnx = bn.x
        fbxgrp(id).vertices(i).bny = bn.y
        fbxgrp(id).vertices(i).bnz = bn.z

    End Sub
    Public Function toFBXv(ByVal inv As vect3) As FbxVector4
        Dim v As New FbxVector4
        v.X = inv.x
        v.Y = inv.y
        v.Z = inv.z
        Return v
    End Function

    Public Sub ComputeTangentBasis(
   ByVal p1 As vect3, ByVal p2 As vect3, ByVal p3 As vect3,
   ByVal UV1 As vect3, ByVal UV2 As vect3, ByVal UV3 As vect3,
   ByRef tangent As vect3, ByRef bitangent As vect3)

        Dim Edge1 As vect3 = subvect3(p2, p1)
        Dim Edge2 As vect3 = subvect3(p3, p1)
        Dim Edge1uv As vect3 = subvect2(UV2, UV1)
        Dim Edge2uv As vect3 = subvect2(UV3, UV1)

        Dim cp As Single = Edge1uv.y * Edge2uv.x - Edge1uv.x * Edge2uv.y

        If cp <> 0.0F Then
            Dim mul As Single = 1.0F / cp
            tangent = mulvect3(addvect3(mulvect3(Edge1, -Edge2uv.y), mulvect3(Edge2, Edge1uv.y)), mul)
            bitangent = mulvect3(addvect3(mulvect3(Edge1, -Edge2uv.x), mulvect3(Edge2, Edge1uv.x)), mul)

            tangent = normalize(tangent)
            bitangent = normalize(bitangent)
        End If

    End Sub
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

    Private Sub build_fbx_matrix(ByVal idx As Integer, ByVal fm As FbxXMatrix)
        ReDim fbxgrp(idx).matrix(15)
        For i = 0 To 15
            fbxgrp(idx).matrix(i) = CSng(fm((i >> 2 And &H3), (i And &H3)))
        Next

    End Sub
    ' Function to convert a normalized float to an 8-bit integer
    Private Function s_to_int(ByVal n As Single) As Byte
        ' Scale n from [-1.0, 1.0] to [0, 254]
        Dim scaled As Double = ((n + 1.0) * 0.5) * 254.0

        ' Round to the nearest integer
        Dim index As Integer = CInt(Math.Round(scaled))

        ' Clamp the value to [0, 254] to prevent out-of-range indices
        If index < 0 Then
            index = 0
        ElseIf index > 254 Then
            index = 254
        End If

        ' Invert the scaled value as per the original lookup logic
        Return CByte(254 - index)
    End Function
    'Private Function s_to_int(ByRef n As Single) As Int32
    '    Dim i As Int32
    '    i = lookup(((n + 1.0) * 0.5) * 254)
    '    Return i
    'End Function

    Public Function packnormalFBX_old(ByVal n As FbxVector4) As UInt32
        'ctz is my special C++ function to pack the vector into a Uint32
        ctz.init_x(n.X)
        ctz.init_y(n.Y)
        ctz.init_z(n.Z)
        Return ctz.pack(1)
    End Function

    Public Function fbx_create_material(pManager As FbxSdkManager, id As Integer) As FbxSurfacePhong
        Dim lMaterial As FbxSurfacePhong
        Dim m_name As String = "Material"
        Dim s_name As String = "Phong"
        'need colors defined
        Dim EmissiveColor = New FbxDouble3(0.0, 0.0, 0.0)
        Dim TransparencyColor = New FbxDouble3(0.0, 0.0, 0.0)
        Dim AmbientColor = New FbxDouble3(0.4, 0.4, 0.4)
        Dim SpecularColor = New FbxDouble3(0.7, 0.7, 0.7)
        Dim DiffuseColor As New FbxDouble3(0.8, 0.8, 0.8)
        'Need a name for this material
        lMaterial = FbxSurfacePhong.Create(pManager, m_name + "" + id.ToString("000"))
        lMaterial.Name = m_name + "" + id.ToString("000")
        lMaterial.EmissiveColor = EmissiveColor
        lMaterial.AmbientColor = AmbientColor
        lMaterial.DiffuseColor = DiffuseColor
        lMaterial.DiffuseFactor = 70.0
        lMaterial.AmbientFactor = 40.0
        lMaterial.SpecularColor = SpecularColor
        lMaterial.SpecularFactor = 0.1
        lMaterial.TransparencyFactor = 0.0
        lMaterial.TransparentColor = TransparencyColor
        lMaterial.Shininess = 90.0
        lMaterial.ShadingModel = s_name
        Return lMaterial
    End Function
    Public Function fbx_create_material_blender(pManager As FbxSdkManager, id As Integer, ByVal n As String) As FbxSurfacePhong
        Dim lMaterial As FbxSurfacePhong
        Dim m_name As String = "Material"
        Dim s_name As String = "Phong"
        'need colors defined
        Dim EmissiveColor = New FbxDouble3(0.0, 0.0, 0.0)
        Dim TransparencyColor = New FbxDouble3(0.0, 0.0, 0.0)
        Dim AmbientColor = New FbxDouble3(0.9, 0.9, 0.9)
        Dim SpecularColor = New FbxDouble3(0.7, 0.7, 0.7)
        Dim DiffuseColor As New FbxDouble3(0.8, 0.8, 0.8)
        'Need a name for this material
        lMaterial = FbxSurfacePhong.Create(pManager, m_name + "" + id.ToString("000"))
        lMaterial.EmissiveColor = EmissiveColor
        lMaterial.AmbientColor = AmbientColor
        lMaterial.DiffuseColor = DiffuseColor
        lMaterial.DiffuseFactor = 100.0
        lMaterial.AmbientFactor = 100.0
        lMaterial.SpecularColor = SpecularColor
        lMaterial.SpecularFactor = 0.3
        lMaterial.TransparencyFactor = 0.0
        lMaterial.TransparentColor = TransparencyColor
        lMaterial.Shininess = 60.0
        lMaterial.ShadingModel = s_name
        Return lMaterial
    End Function
    Public Function fbx_create_Vmaterial(pManager As FbxSdkManager, id As Integer) As FbxSurfacePhong
        Dim lMaterial As FbxSurfacePhong
        Dim m_name As String = "Marker_Material"
        Dim s_name As String = "Phong"
        'need colors defined
        Dim v As New vColor_
        Select Case id
            Case 0
                v = vc4
            Case 1
                v = vc1
            Case 2
                v = vc2
            Case 3
                v = vc3
            Case 4
                v = vc0
            Case 5
                v = vc5
        End Select
        Dim EmissiveColor = New FbxDouble3(0.0, 0.0, 0.0)
        Dim AmbientColor = New FbxDouble3(v.r, v.g, v.b)
        Dim SpecularColor = New FbxDouble3(0.7, 0.7, 0.7)
        Dim DiffuseColor As New FbxDouble3(v.r * 0.8, v.g * 0.8, v.b * 0.8)
        'Need a name for this material
        lMaterial = FbxSurfacePhong.Create(pManager, m_name + "" + id.ToString("000"))
        lMaterial.EmissiveColor = EmissiveColor
        lMaterial.AmbientColor = AmbientColor
        lMaterial.DiffuseColor = AmbientColor
        lMaterial.SpecularColor = SpecularColor
        lMaterial.SpecularFactor = 0.3
        lMaterial.TransparencyFactor = 0.0
        lMaterial.Shininess = 60.0
        lMaterial.ShadingModel = s_name
        Return lMaterial
    End Function
    Public Function fbx_create_Vmaterial_blender(pManager As FbxSdkManager, id As Integer, ByRef name As String) As FbxSurfacePhong
        Dim lMaterial As FbxSurfacePhong
        Dim m_name As String = "Marker_Material"
        Dim s_name As String = "Phong"
        'need colors defined
        Dim v As New vColor_
        Select Case id
            Case 0
                v = vc4
            Case 1
                v = vc1
            Case 2
                v = vc2
            Case 3
                v = vc3
            Case 4
                v = vc0
            Case 5
                v = vc5
        End Select
        Dim EmissiveColor = New FbxDouble3(0.0, 0.0, 0.0)
        Dim AmbientColor = New FbxDouble3(v.r, v.g, v.b)
        Dim SpecularColor = New FbxDouble3(0.7, 0.7, 0.7)
        Dim DiffuseColor As New FbxDouble3(v.r * 0.8, v.g * 0.8, v.b * 0.8)
        'Need a name for this material
        lMaterial = FbxSurfacePhong.Create(pManager, m_name + "" + id.ToString("000") + "_" + name)
        lMaterial.EmissiveColor = EmissiveColor
        lMaterial.AmbientColor = AmbientColor
        lMaterial.DiffuseColor = AmbientColor
        lMaterial.SpecularColor = SpecularColor
        lMaterial.SpecularFactor = 0.3
        lMaterial.TransparencyFactor = 0.0
        lMaterial.Shininess = 60.0
        lMaterial.ShadingModel = s_name
        Return lMaterial
    End Function

    Public Function fbx_create_texture_primtive(pManager As FbxSdkManager, id As Integer) As FbxTexture
        'need a name for this texture
        Dim texture As FbxTexture
        texture = FbxTexture.Create(pManager, "Texture_" + id.ToString)
        Dim t_name As String = ""
        ' Set texture properties.
        Try

            If _group(id).is_atlas_type Then
                Dim idx = _group(id).g_atlas_indexs.x
                t_name = FBX_Texture_path + "\" + "Atlas_AM_map_" + id.ToString + ".png"
            Else
                t_name = FBX_Texture_path + "\" +
                                            Path.GetFileNameWithoutExtension(_group(id).color_name) + ".png"
            End If
        Catch ex As Exception

        End Try

        texture.SetFileName(t_name) 'Get the Texture path from the list
        texture.TextureUseType = FbxTexture.TextureUse.Standard
        texture.Mapping = FbxTexture.MappingType.Uv
        texture.MaterialUseType = FbxTexture.MaterialUse.Model
        texture.SwapUV = False
        texture.SetTranslation(0.0, 0.0)
        texture.SetScale(1.0, 1.0)
        texture.SetRotation(0.0, 0.0)
        Return texture
    End Function
    Public Function fbx_create_texture(pManager As FbxSdkManager, id As Integer) As FbxTexture
        'need a name for this texture
        Dim texture As FbxTexture
        texture = FbxTexture.Create(pManager, FBX_Texture_path + "\" + Path.GetFileNameWithoutExtension(textures(id).c_name) + ".png")
        ' Set texture properties.
        texture.SetFileName(FBX_Texture_path + "\" + Path.GetFileNameWithoutExtension(textures(id).c_name) + ".png") 'Get the Texture path from the list
        texture.TextureUseType = FbxTexture.TextureUse.Standard
        texture.Mapping = FbxTexture.MappingType.Uv
        texture.MaterialUseType = FbxTexture.MaterialUse.Model
        texture.SwapUV = False
        texture.SetTranslation(0.0, 0.0)
        texture.SetScale(1.0, 1.0)
        texture.SetRotation(0.0, 0.0)
        Return texture
    End Function

    Public Function fbx_create_texture_N_primitive(pManager As FbxSdkManager, id As Integer) As FbxTexture
        'need a name for this texture
        Dim texture As FbxTexture
        texture = FbxTexture.Create(pManager, "nTexture_" + id.ToString)
        Dim t_name As String = ""
        Try

            If _group(id).is_atlas_type Then
                Dim idx = _group(id).g_atlas_indexs.x
                t_name = FBX_Texture_path + "\" + "Atlas_NM_map_" + id.ToString + ".png"
            Else
                t_name = FBX_Texture_path + "\" +
                                            Path.GetFileNameWithoutExtension(_group(id).normal_name) + ".png"
            End If
        Catch ex As Exception

        End Try
        ' Set texture properties.
        texture.SetFileName(t_name) 'Get the Texture path from the list
        texture.TextureUseType = FbxTexture.TextureUse.BumpNormalMap
        texture.Mapping = FbxTexture.MappingType.Uv
        texture.MaterialUseType = FbxTexture.MaterialUse.Model
        texture.SwapUV = False
        texture.SetTranslation(0.0, 0.0)
        texture.SetScale(1.0, 1.0)
        texture.SetRotation(0.0, 0.0)
        Return texture
    End Function
    Public Function fbx_create_texture_N(pManager As FbxSdkManager, id As Integer) As FbxTexture
        'need a name for this texture
        'Dim texture = FbxTexture.Create(pManager, "NormalMap" + ":" + id.ToString("000"))
        Dim texture = FbxTexture.Create(pManager, FBX_Texture_path + "\" + Path.GetFileNameWithoutExtension(textures(id).n_name) + ".png")
        ' Set texture properties.
        texture.SetFileName(FBX_Texture_path + "\" + Path.GetFileNameWithoutExtension(textures(id).n_name) + ".png") 'Get the Texture path from the list
        texture.TextureUseType = FbxTexture.TextureUse.BumpNormalMap
        texture.Mapping = FbxTexture.MappingType.Uv
        texture.MaterialUseType = FbxTexture.MaterialUse.Model
        texture.SwapUV = False
        texture.SetTranslation(0.0, 0.0)
        texture.SetScale(1.0, 1.0)
        texture.SetRotation(0.0, 0.0)
        Return texture
    End Function

    Private Function load_matrix_decompose(data() As Double, ByRef trans As SharpDX.Vector3, ByRef scale As SharpDX.Vector3, ByRef rot As SharpDX.Quaternion) As SharpDX.Matrix
        Dim m_ As New SharpDX.Matrix
        For i = 0 To 3
            For k = 0 To 3
                m_(i, k) = data((i * 4) + k)
            Next
        Next

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

    Public Function fbx_create_mesh(model_name As String, id As Integer, pManager As FbxSdkManager) As FbxMesh
        Dim myMesh As FbxMesh
        myMesh = FbxMesh.Create(pManager, model_name)
        Dim cnt = _group(id).nPrimitives_
        Dim off As UInt32
        Dim v As vect3Norm
        Dim v4 As New FbxVector4
        Dim I As Integer
        off = _group(id).startVertex_

        '--------------------------------------------------------------------------
        '--------------------------------------------------------------------------
        'first we load all the vertices for the _group data
        myMesh.InitControlPoints(_group(id).nVertices_) ' size of array
        'add in the vertices (or control points as its called in FBX)
        Dim cp_array(myMesh.ControlPointsCount - 1) As FbxVector4

        For I = 0 To myMesh.ControlPointsCount - 1
            cp_array(I) = New FbxVector4
            cp_array(I).X = _group(id).vertices(I).x
            cp_array(I).Y = _group(id).vertices(I).y
            cp_array(I).Z = _group(id).vertices(I).z
        Next

        myMesh.ControlPoints = cp_array ' push it in to the mesh object
        'create or get the layer 0
        Dim layer As FbxLayer = myMesh.GetLayer(0)
        If layer Is Nothing Then
            myMesh.CreateLayer()
            layer = myMesh.GetLayer(0)
        End If

        '--------------------------------------------------------------------------
        '--------------------------------------------------------------------------
        'normals.. seems to be working ok
        Dim layerElementNormal = FbxLayerElementNormal.Create(myMesh, "Normals")
        layerElementNormal.Mapping_Mode = FbxLayerElement.MappingMode.ByPolygonVertex
        layerElementNormal.Reference_Mode = FbxLayerElement.ReferenceMode.Direct
        'time to assign the normals to each control point.

        For I = 1 To _group(id).nPrimitives_
            Dim v1 = _group(id).indices(I).v1
            Dim v2 = _group(id).indices(I).v2
            Dim v3 = _group(id).indices(I).v3
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
        'export vertex colors
        If _group(id).header.Contains("iii") Then ' has indices
            Dim colorLayer1 As FbxLayerElementVertexColor = Nothing
            colorLayer1 = FbxLayerElementVertexColor.Create(myMesh, "VertexColor")
            colorLayer1.Name = "VertexColor"
            colorLayer1.Mapping_Mode = FbxLayerElement.MappingMode.ByControlPoint
            colorLayer1.Reference_Mode = FbxLayerElement.ReferenceMode.Direct
            Dim color As New FbxColor
            For I = 1 To _group(id).nPrimitives_
                Dim indi = _group(id).indices(I)
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
        Dim v_2 As New FbxVector2
        Dim UV2Layer As FbxLayerElementUV = Nothing
        If _group(id).has_uv2 = 1 Then

            UV2Layer = FbxLayerElementUV.Create(myMesh, "UV2")
            UV2Layer.Mapping_Mode = FbxLayerElement.MappingMode.ByControlPoint
            UV2Layer.Reference_Mode = FbxLayerElement.ReferenceMode.Direct
            layer.SetUVs(UV2Layer, FbxLayerElement.LayerElementType.AmbientTextures)
            For I = 0 To myMesh.ControlPointsCount - 1
                If frmFBX.flip_u.Checked Then
                    v_2.X = _group(id).vertices(I).u2 * -1
                Else
                    v_2.X = _group(id).vertices(I).u2
                End If

                If frmFBX.flip_v.Checked Then
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
        Dim UVDiffuseLayer As FbxLayerElementUV = FbxLayerElementUV.Create(myMesh, "DiffuseUV")
        UVDiffuseLayer.Mapping_Mode = FbxLayerElement.MappingMode.ByControlPoint
        UVDiffuseLayer.Reference_Mode = FbxLayerElement.ReferenceMode.Direct
        layer.SetUVs(UVDiffuseLayer, FbxLayerElement.LayerElementType.DiffuseTextures)
        For I = 0 To myMesh.ControlPointsCount - 1
            If frmFBX.flip_u.Checked Then
                v_2.X = _group(id).vertices(I).u * -1
            Else
                v_2.X = _group(id).vertices(I).u
            End If

            If Not frmFBX.flip_v.Checked Then
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
            pos = _group(id).indices(n).v1 - off
            myMesh.AddPolygon(pos)
            UVDiffuseLayer.IndexArray.SetAt(pos, j)
            If _group(id).has_uv2 = 1 Then
                UV2Layer.IndexArray.SetAt(pos, j)
            End If
            j += 1
            pos = _group(id).indices(n).v2 - off
            myMesh.AddPolygon(pos)
            UVDiffuseLayer.IndexArray.SetAt(pos, j)
            If _group(id).has_uv2 = 1 Then
                UV2Layer.IndexArray.SetAt(pos, j)
            End If
            j += 1
            pos = _group(id).indices(n).v3 - off
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
    Public Function fbx_create_primi_mesh(model_name As String, id As Integer, pManager As FbxSdkManager) As FbxMesh
        Dim myMesh As FbxMesh
        myMesh = FbxMesh.Create(pManager, model_name)

        Dim cnt = _group(id).nPrimitives_
        Dim off As UInt32
        Dim v As vect3Norm
        Dim v4 As New FbxVector4
        Dim I As Integer
        off = _group(id).startVertex_

        '--------------------------------------------------------------------------
        '--------------------------------------------------------------------------
        'first we load all the vertices for the _group data
        myMesh.InitControlPoints(_group(id).nVertices_) ' size of array
        'add in the vertices (or control points as its called in FBX)
        Dim cp_array(myMesh.ControlPointsCount - 1) As FbxVector4

        For I = 0 To myMesh.ControlPointsCount - 1
            cp_array(I) = New FbxVector4
            cp_array(I).X = _group(id).vertices(I).x
            cp_array(I).Y = _group(id).vertices(I).y
            cp_array(I).Z = _group(id).vertices(I).z
        Next

        myMesh.ControlPoints = cp_array ' push it in to the mesh object
        'create or get the layer 0
        Dim layer As FbxLayer = myMesh.GetLayer(0)
        If layer Is Nothing Then
            myMesh.CreateLayer()
            layer = myMesh.GetLayer(0)
        End If

        '--------------------------------------------------------------------------
        '--------------------------------------------------------------------------
        'normals.. seems to be working ok
        Dim layerElementNormal = FbxLayerElementNormal.Create(myMesh, "Normals")
        layerElementNormal.Mapping_Mode = FbxLayerElement.MappingMode.ByPolygonVertex
        layerElementNormal.Reference_Mode = FbxLayerElement.ReferenceMode.Direct
        'time to assign the normals to each control point.

        For I = 1 To _group(id).nPrimitives_
            Dim v1 = _group(id).indices(I).v1
            Dim v2 = _group(id).indices(I).v2
            Dim v3 = _group(id).indices(I).v3
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
            Dim colorLayer1 As FbxLayerElementVertexColor = Nothing
            colorLayer1 = FbxLayerElementVertexColor.Create(myMesh, "VertexColor")
            colorLayer1.Name = "VertexColor"
            colorLayer1.Mapping_Mode = FbxLayerElement.MappingMode.ByControlPoint
            colorLayer1.Reference_Mode = FbxLayerElement.ReferenceMode.Direct
            Dim color As New FbxColor
            For I = 1 To _group(id).nPrimitives_
                Dim indi = _group(id).indices(I)
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
        Dim UVDiffuseLayer As FbxLayerElementUV = FbxLayerElementUV.Create(myMesh, "DiffuseUV")
        Dim v_2 As New FbxVector2
        If _group(id).is_atlas_type Then
            'use UV2 mapping
            If _group(id).has_uv2 = 1 Then
                UVDiffuseLayer.Mapping_Mode = FbxLayerElement.MappingMode.ByControlPoint
                UVDiffuseLayer.Reference_Mode = FbxLayerElement.ReferenceMode.Direct
                layer.SetUVs(UVDiffuseLayer, FbxLayerElement.LayerElementType.DiffuseTextures)
                For I = 0 To myMesh.ControlPointsCount - 1
                    If frmFBX.flip_u.Checked Then
                        v_2.X = _group(id).vertices(I).u2 * -1
                    Else
                        v_2.X = _group(id).vertices(I).u2
                    End If

                    If frmFBX.flip_v.Checked Then
                        v_2.Y = _group(id).vertices(I).v2 * -1
                    Else
                        v_2.Y = _group(id).vertices(I).v2
                    End If
                    UVDiffuseLayer.DirectArray.Add(v_2)

                Next
            End If
        Else
            'use UV1 mapping
            UVDiffuseLayer.Mapping_Mode = FbxLayerElement.MappingMode.ByControlPoint
            UVDiffuseLayer.Reference_Mode = FbxLayerElement.ReferenceMode.Direct
            layer.SetUVs(UVDiffuseLayer, FbxLayerElement.LayerElementType.DiffuseTextures)
            For I = 0 To myMesh.ControlPointsCount - 1
                If frmFBX.flip_u.Checked Then
                    v_2.X = _group(id).vertices(I).u * -1
                Else
                    v_2.X = _group(id).vertices(I).u
                End If

                If Not frmFBX.flip_v.Checked Then
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
            pos = _group(id).indices(n).v1 - off
            myMesh.AddPolygon(pos)
            UVDiffuseLayer.IndexArray.SetAt(pos, j)
            j += 1
            pos = _group(id).indices(n).v2 - off
            myMesh.AddPolygon(pos)
            UVDiffuseLayer.IndexArray.SetAt(pos, j)
            j += 1
            pos = _group(id).indices(n).v3 - off
            myMesh.AddPolygon(pos)
            UVDiffuseLayer.IndexArray.SetAt(pos, j)
            n += 1
            myMesh.EndPolygon()

        Next
        Return myMesh
    End Function
    Public Function fbx_create_Vmesh(model_name As String, pManager As FbxSdkManager, ByRef vm As v_marker_) As FbxMesh
        Dim myMesh As FbxMesh
        myMesh = FbxMesh.Create(pManager, model_name)
        Dim cnt = vm.indice_count - 1
        Dim v As vec3
        Dim v4 As New FbxVector4
        Dim I As Integer

        '--------------------------------------------------------------------------
        '--------------------------------------------------------------------------
        'first we load all the vertices for the _group data
        myMesh.InitControlPoints(vm.vertice_count) ' size of array
        'add in the vertices (or control points as its called in FBX)
        Dim cp_array(myMesh.ControlPointsCount - 1) As FbxVector4
        For I = 0 To myMesh.ControlPointsCount - 1
            cp_array(I) = New FbxVector4
            cp_array(I).X = vm.vertices(I).x
            cp_array(I).Y = vm.vertices(I).y
            cp_array(I).Z = vm.vertices(I).z
        Next
        myMesh.ControlPoints = cp_array ' push it in to the mesh object
        'create or get the layer 0
        Dim layer As FbxLayer = myMesh.GetLayer(0)
        If layer Is Nothing Then
            myMesh.CreateLayer()
            layer = myMesh.GetLayer(0)
        End If

        '--------------------------------------------------------------------------
        '--------------------------------------------------------------------------
        'normals.. seems to be working ok
        Dim layerElementNormal = FbxLayerElementNormal.Create(myMesh, "Normals")
        layerElementNormal.Mapping_Mode = FbxLayerElement.MappingMode.ByPolygonVertex
        layerElementNormal.Reference_Mode = FbxLayerElement.ReferenceMode.Direct
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

    Public Function packnormalFBX888_(ByVal n As FbxVector4) As UInt32
        ' Normalize the components to the range [-127, 127]
        Dim x As Int32 = CInt(Math.Round(n.X * 127))
        Dim y As Int32 = CInt(Math.Round(n.Y * 127))
        Dim z As Int32 = CInt(Math.Round(n.Z * 127))

        ' Convert signed values to unsigned (range [0, 255])
        x = (x And &HFF) Xor 127
        y = (y And &HFF) Xor 127
        z = (z And &HFF) Xor 127

        ' Pack the components into a UInt32
        Dim packed As UInt32 = (CUInt(z) << 16) Or (CUInt(y) << 8) Or CUInt(x)
        Return packed
    End Function
    Public Function packnormalFBX888(ByVal n As FbxVector4) As UInt32
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
    Public Function packnormalFBX888_writePrimitive(ByVal n As FbxVector4) As UInt32
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
    Public Function packnormalFBX888_writePrimitive_NEWMODEL(ByVal n As FbxVector4) As UInt32
        Try
            ' Normalize the vector

            ' Round each component to 4 decimal places
            n.X = CSng(Math.Round(n.X, 4))
            n.Y = CSng(Math.Round(n.Y, 4))
            n.Z = CSng(Math.Round(n.Z, 4))
            n.Normalize()

            ' Convert each component to an 8-bit integer and print input-output values
            Dim nx As Byte = s_to_int(n.X)
            Dim ny As Byte = s_to_int(n.Y)
            Dim nz As Byte = s_to_int(n.Z)
            ' Pack the components into a UInt32 (Format: 0x00ZZYYXX)
            Dim packedValue As UInt32 = CUInt((CInt(nz) << 16) Or (CInt(ny) << 8) Or CInt(nx))

            Return packedValue
        Catch ex As Exception
            ' Handle exceptions appropriately (e.g., log the error)
            Console.WriteLine($"Error in packing function: {ex.Message}")
            Return 0
        End Try
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
