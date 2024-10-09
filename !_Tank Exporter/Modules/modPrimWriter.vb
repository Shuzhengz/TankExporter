﻿
Imports System.IO
Imports System.Windows
Imports System.Runtime.InteropServices
Imports System.Text
Imports SharpGLTF.Schema2
Imports Assimp.Configs

Module modPrimWriter
    Public OBJECT_WAS_INSERTED As Boolean
    Public CHASSIS_COUNT As Integer
    Dim br As BinaryWriter
    Dim pnter As Integer
    Dim b As Byte = &H0
    Dim indi_cnt As Integer
    Dim obj_cnt As Integer
    Dim total_indices As Integer
    Dim bsp2_mat_size As Integer
    Dim bsp2_size As Integer
    Dim idx_size As Integer
    Dim uv2_cnt As Integer
    Dim total_verts As Integer

    Dim l As Integer
    Dim padding As Integer
    Dim Ipadding As Integer
    Dim Vpadding As Integer
    Dim UV2padding As Integer

    Public Sub hide_tracks()
        If CRASH_MODE Then
            Return 'no sense in hiding the non-crashed models tracks.
        End If
        Dim path = My.Settings.res_mods_path
        Dim a = m_groups(1).f_name(0).ToLower.Split("normal")
        Dim p = path + "\" + a(0) + "track\"
        If Directory.Exists(p) Then 'make sure the directory exist!
            Dim d As New DirectoryInfo(p)
            Dim di = d.GetFiles
            'We are going to replace the existing segments with the blank one.
            Dim segment = File.ReadAllBytes(Application.StartupPath + "\resources\primitive\segment.blank")
            For Each n In di
                If n.Name.Contains("primitives_pr") Then
                    File.WriteAllBytes(n.FullName, segment)
                End If
            Next
            ReDim segment(0) 'clean up 
            GC.Collect()
            For Each n In di
                If n.Name.Contains("_ANM.") Then
                    path = n.FullName
                    GoTo found_it
                End If
            Next
            MsgBox("I didn't find the ANM file under the track folder!", MsgBoxStyle.Exclamation, "Well Shit!")
            Return
found_it:
            Dim sr_file = Application.StartupPath + "\resources\blank_dds.dds"
            File.Delete(path)
            File.Copy(sr_file, path)
            Try
                path = path.ToLower.Replace("anm.dds", "ANM_hd.dds")
                File.Delete(path)
                File.Copy(sr_file, path)
            Catch ex As Exception

            End Try

            For Each n In di
                If n.Name.Contains(".visual_processed") Then
                    path = n.FullName
                    Dim ok = get_xml_in_resmods(path)
                    If ok Then

                        Dim s = TheXML_String.Replace(vbCrLf, vbLf)
                        s = s.Replace("  ", "")
                        Dim ar = s.Split(vbLf)
                        For i = 0 To ar.Length - 1
                            If ar(i).ToLower.Contains("alphatestenable") Then
                                ar(i) = ar(i).Replace("false", "true")
                            End If
                            If ar(i).ToLower.Contains("alphareference") Then
                                ar(i) = ar(i).Replace("0", "192")
                            End If
                        Next
                        Dim out_file As String = ar(0)
                        For i = 1 To ar.Length - 1
                            out_file += vbLf
                            out_file += ar(i)
                        Next
                        For i = 0 To 100
                            out_file = out_file.Replace("<primitiveGroup>" + vbLf + "<PG_ID>" + i.ToString + "</PG_ID>", "<primitiveGroup>" + i.ToString)
                        Next
                        s = ""

                        out_file = out_file.Replace("SceneRoot", "Scene Root")
                        out_file = out_file.Replace("map_", IO.Path.GetFileName(path))
                        File.WriteAllText(path, out_file)
                    End If 'ok
                End If
            Next
        Else
            MsgBox("You need to extract the files before writing!", MsgBoxStyle.Exclamation, "Well Shit!")
        End If

        GC.Collect()
    End Sub
    Private Function get_xml_in_resmods(ByVal path As String) As Boolean
        Try

            Dim mstream = New MemoryStream
            Dim buf = File.ReadAllBytes(path)
            mstream = New MemoryStream(buf)
            If openXml_stream(mstream, "") Then
                mstream.Dispose()
                buf = Nothing
                Return True
            End If
            GC.Collect()
        Catch ex As Exception
            Return False

        End Try
        Return False

    End Function

    Public Sub write_chassis_crashed(ByVal id As Integer)
        Dim i As UInt32
        Dim dummy As UInt32 = 0
        'Return
        Dim table(20000) As Byte
        Dim table_size As UInteger = 0
        Dim r As FileStream = Nothing
        obj_cnt = m_groups(id).cnt
        Try
            r = New FileStream(My.Settings.res_mods_path + "\" + m_groups(id).f_name(0), FileMode.Create, FileAccess.Write)
        Catch e As Exception
            MsgBox("I could not open """ + My.Settings.res_mods_path + "\" + m_groups(id).f_name(0) + """!" + vbCrLf +
                    "The Root folder is there but there are no  .primitive_processed files." + vbCrLf _
                    + " Did you delete them?", MsgBoxStyle.Exclamation, "Can find folder!")
            Return
        End Try
        br = New BinaryWriter(r)
        Dim mh As UInt32 = &H42A14E65
        br.Write(mh) ' write magic number
        '-------------------------------------------------------------
        Dim p = br.BaseStream.Position - 4
        Dim ms_table As New MemoryStream(table)
        Dim t_writer As New BinaryWriter(ms_table)

        For k = 0 To section_names(1).names.Length - 1
            For section = 1 To CHASSIS_COUNT
                Dim pnt_id = (m_groups(id).group_list.Length - m_groups(id).group_list(section - 1)) + 1
                '================================================================ TRACK
                'All data for the tracks comes from the original file. There is no reason to 
                'change any of this as it cant be edited anyway!
                If _group(pnt_id).table_entry_name.ToLower.Contains("track") And
                    _group(pnt_id).table_entry_name = section_names(1).names(k) Then
                    '-------------------------------------------------------------
                    frmMain.info_Label.Text = "Compacting Data ID=" + pnt_id.ToString
                    Application.DoEvents()
                    Dim comp As comp_ = compact_primitive(pnt_id, fbxgrp(pnt_id).comp)
                    '-------------------------------------------------------------
                    'save current position
                    Dim sect_start = br.BaseStream.Position
                    'assuming there will never be a "list2" in these files!
                    Dim n = System.Text.Encoding.Default.GetBytes("list")
                    ReDim Preserve n(63) 'pad by resizing
                    br.Write(n) ' save string as padded binary
                    br.Write(comp.indi_cnt)
                    br.Write(1) ' write group count
                    'write indices
                    Dim c As Integer = 0
                    Try
                        For i = 0 To comp.indi_cnt - 1 Step 3
                            c = i + 2
                            If Not frmWritePrimitive.flipWindingOrder_cb.Checked Then
                                br.Write(Convert.ToUInt16(comp.indices(i + 1)))
                                br.Write(Convert.ToUInt16(comp.indices(i + 0)))
                                br.Write(Convert.ToUInt16(comp.indices(i + 2)))
                            Else
                                br.Write(Convert.ToUInt16(comp.indices(i + 0)))
                                br.Write(Convert.ToUInt16(comp.indices(i + 1)))
                                br.Write(Convert.ToUInt16(comp.indices(i + 2)))
                            End If
                        Next
                    Catch ex As Exception
                        MsgBox("failed writing indices OBJ:" + _group(pnt_id).name, MsgBoxStyle.Exclamation, "Fail!")
                        ms_table.Close()
                        t_writer.Close()
                        t_writer.Dispose()
                        r.Close()
                        br.Close()
                        br.Dispose()
                        Return

                    End Try

                    'Write entry for each model in this group
                    'tracks and chassis only have ONE per entry
                    br.Write(CInt(0)) ' indices start index
                    br.Write(CUInt(comp.nPrimitives)) ' primitive count
                    br.Write(CInt(0)) ' vertices start index
                    br.Write(CUInt(comp.vert_cnt))

                    p = br.BaseStream.Position - sect_start
                    l = (br.BaseStream.Position) Mod 4L
                    padding = CUInt(l)
                    'writing padding bytes if needed
                    If l > 0 Then
                        For i = 1 To 4 - l
                            br.Write(b)
                        Next
                    End If
                    'first entry in table at end ==================================================
                    t_writer.Write(CUInt(p))
                    t_writer.Write(New Long)
                    t_writer.Write(New Long)
                    Dim r_name = Path.GetFileNameWithoutExtension(_group(pnt_id).table_entry_name) + ".indices"
                    n = System.Text.Encoding.Default.GetBytes(r_name)
                    t_writer.Write(CInt(n.Length))
                    t_writer.Write(n)
                    l = (t_writer.BaseStream.Position) Mod 4L
                    If l > 0 Then
                        For i = 1 To 4 - l
                            t_writer.Write(b)
                        Next
                    End If

                    sect_start = br.BaseStream.Position

                    Dim h1() = "BPVTxyznuviiiwwtb".ToArray
                    Dim h2() = "set3/xyznuviiiwwtbpc".ToArray
                    ReDim Preserve h1(67)
                    ReDim Preserve h2(63)
                    br.Write(h1)
                    br.Write(h2)
                    'write primitive count
                    br.Write(comp.vert_cnt)
                    Try
                        Dim v As vect3
                        For i = 0 To comp.vert_cnt - 1
                            v.x = comp.vertices(i).x
                            v.y = comp.vertices(i).y
                            v.z = comp.vertices(i).z
                            v = rotate_scale_translate_transform(v, fbxgrp(pnt_id).matrix)
                            br.Write(v.x)
                            br.Write(v.y)
                            br.Write(v.z)

                            br.Write(comp.vertices(i).n)
                            br.Write(comp.vertices(i).u)
                            br.Write(comp.vertices(i).v)

                            br.Write(comp.vertices(i).index_1)
                            br.Write(comp.vertices(i).index_2)
                            br.Write(comp.vertices(i).index_3)

                            br.Write(CByte(0))

                            br.Write(CByte(0))
                            br.Write(CByte(0))
                            'br.Write(_group(pnt_id).vertices(i).weight_3)
                            br.Write(CByte(255))
                            br.Write(CByte(0))

                            br.Write(comp.vertices(i).t)
                            br.Write(comp.vertices(i).bn)


                        Next
                    Catch ex As Exception
                        MsgBox("failed writing vertices OBJ:" + _group(pnt_id).name, MsgBoxStyle.Exclamation, "Fail!")
                        ms_table.Close()
                        t_writer.Close()
                        t_writer.Dispose()
                        r.Close()
                        br.Close()
                        br.Dispose()
                        Return

                    End Try
                    p = br.BaseStream.Position - sect_start
                    l = (br.BaseStream.Position) Mod 4L
                    Dim padding2 = CUInt(l)
                    'writing padding bytes if needed
                    If l > 0 Then
                        For i = 1 To 4 - l
                            br.Write(b)
                        Next
                    End If
                    'second entry in table at end ==================================================
                    'write entry in table
                    t_writer.Write(CUInt(p))
                    t_writer.Write(New Long)
                    t_writer.Write(New Long)
                    r_name = Path.GetFileNameWithoutExtension(_group(pnt_id).table_entry_name) + ".vertices"
                    n = System.Text.Encoding.Default.GetBytes(r_name)
                    t_writer.Write(CInt(n.Length))
                    t_writer.Write(n)
                    l = (t_writer.BaseStream.Position) Mod 4L
                    If l > 0 Then
                        For i = 1 To 4 - l
                            t_writer.Write(b)
                        Next
                    End If


                    sect_start = br.BaseStream.Position
                    'write uv2s
                    'write UV2 section header
                    Dim h3() = "BPVSuv2".ToArray
                    Dim h4() = "set3/uv2pc".ToArray
                    ReDim Preserve h3(67)
                    ReDim Preserve h4(63)
                    br.Write(h3)
                    br.Write(h4)
                    'write count
                    br.Write(_group(pnt_id).nVertices_)
                    'write UV2s
                    For i = 0 To _group(pnt_id).nVertices_ - 1
                        br.Write(_group(pnt_id).vertices(i).u2)
                        br.Write(_group(pnt_id).vertices(i).v2)
                    Next
                    'third entry in table at end ==================================================
                    'write entry in table
                    p = br.BaseStream.Position - sect_start
                    t_writer.Write(CUInt(p))
                    t_writer.Write(New Long)
                    t_writer.Write(New Long)
                    r_name = Path.GetFileNameWithoutExtension(_group(pnt_id).table_entry_name) + ".uv2"
                    n = System.Text.Encoding.Default.GetBytes(r_name)
                    t_writer.Write(CInt(n.Length))
                    t_writer.Write(n)
                    l = (t_writer.BaseStream.Position) Mod 4L
                    If l > 0 Then
                        For i = 1 To 4 - l
                            t_writer.Write(b)
                        Next
                    End If
                    'section += 1
                End If
            Next
        Next
        ' section names loop
        table_size = t_writer.BaseStream.Position - 1
        ReDim Preserve table(table_size)
        br.Write(table)
        br.Write(table.Length)

        'File.WriteAllBytes("C:\test_table.bin", table) 'write debuging file

        'clean up stuff
        ms_table.Close()
        t_writer.Close()
        t_writer.Dispose()
        r.Close()
        br.Close()
        br.Dispose()

    End Sub

    Public Sub write_chassis_primitives(id)
        Dim i As UInt32
        If False Then ' set true to write out comparision file

            Dim tb As New StringBuilder
            tb.AppendLine("=========================")
            For k = 1 To CHASSIS_COUNT
                Dim c = _group(k).nVertices_
                Dim t = 0
                tb.AppendLine(_group(k).table_entry_name)
                For i = 0 To c - 1
                    If _group(k).vertices(i).index_1 <> fbxgrp(k).vertices(i).index_1 Then

                    End If
                    If _group(k).vertices(i).index_3 <> fbxgrp(k).vertices(i).index_2 Then
                        'Debug.WriteLine("miss match")
                    End If
                    If _group(k).vertices(i).index_2 <> fbxgrp(k).vertices(i).index_3 Then
                        'Debug.WriteLine("miss match")
                    End If
                    tb.AppendLine("G: " + _group(k).vertices(i).index_1.ToString("000") + " " + _group(k).vertices(i).index_2.ToString("000") + "  " + i.ToString("00000"))
                    tb.AppendLine("F: " + fbxgrp(k).vertices(i).index_1.ToString("000") + " " + fbxgrp(k).vertices(i).index_2.ToString("000"))
                Next
                tb.AppendLine("=========================")


            Next
            File.WriteAllText("c:\colorTest.txt", tb.ToString)
            Return
        End If

        Dim dummy As UInt32 = 0
        'Return
        Dim table(20000) As Byte
        Dim table_size As UInteger = 0
        Dim r As FileStream = Nothing
        obj_cnt = m_groups(id).cnt
        Try
            r = New FileStream(My.Settings.res_mods_path + "\" + m_groups(id).f_name(0), FileMode.Create, FileAccess.Write)
        Catch e As Exception
            MsgBox("I could not open """ + My.Settings.res_mods_path + "\" + m_groups(id).f_name(0) + """!" + vbCrLf +
                    "The Root folder is there but there are no  .primitive_processed files." + vbCrLf _
                    + " Did you delete them?", MsgBoxStyle.Exclamation, "Can find folder!")
            Return
        End Try

        br = New BinaryWriter(r)
        Dim mh As UInt32 = &H42A14E65
        br.Write(mh) ' write magic number
        '-------------------------------------------------------------
        Dim p = br.BaseStream.Position - 4
        Dim ms_table As New MemoryStream(table)
        Dim t_writer As New BinaryWriter(ms_table)

        For k = 0 To section_names(1).names.Length - 1
            For section = 1 To CHASSIS_COUNT
                Dim pnt_id = (m_groups(id).group_list.Length - m_groups(id).group_list(section - 1)) + 1
                '================================================================ TRACK
                'All data for the tracks comes from the original file. There is no reason to 
                'change any of this as it cant be edited anyway!
                If _group(pnt_id).table_entry_name.ToLower.Contains("track") And
                    _group(pnt_id).table_entry_name = section_names(1).names(k) Then
                    'save current position
                    Dim sect_start = br.BaseStream.Position
                    'assuming there will never be a "list2" in these files!
                    Dim n = System.Text.Encoding.Default.GetBytes("list")
                    ReDim Preserve n(63) 'pad by resizing
                    br.Write(n) ' save string as padded binary
                    br.Write((_group(pnt_id).indices.Length - 1) * 3)
                    br.Write(1) ' write group count
                    'write indices
                    For i = 1 To _group(pnt_id).indices.Length - 1
                        If Not frmWritePrimitive.flipWindingOrder_cb.Checked Then
                            br.Write(Convert.ToUInt16(_group(pnt_id).indices(i).v2))
                            br.Write(Convert.ToUInt16(_group(pnt_id).indices(i).v1))
                            br.Write(Convert.ToUInt16(_group(pnt_id).indices(i).v3))
                        Else
                            br.Write(Convert.ToUInt16(_group(pnt_id).indices(i).v1))
                            br.Write(Convert.ToUInt16(_group(pnt_id).indices(i).v2))
                            br.Write(Convert.ToUInt16(_group(pnt_id).indices(i).v3))
                        End If
                    Next

                    'Write entry for each model in this group
                    'tracks and chassis only have ONE per entry
                    br.Write(CInt(0)) ' indices start index
                    br.Write(CUInt(_group(pnt_id).indices.Length - 1)) ' indice count
                    br.Write(CInt(0)) ' vertices start index
                    br.Write(CUInt(_group(pnt_id).nVertices_))

                    p = br.BaseStream.Position - sect_start ' get size of this chunk of data
                    l = (br.BaseStream.Position) Mod 4L
                    padding = l
                    'writing padding bytes if needed
                    If l > 0 Then
                        For i = 1 To 4 - l
                            br.Write(b)
                        Next
                    End If
                    'first entry in table at end ==================================================
                    t_writer.Write(CUInt(p))
                    t_writer.Write(New Long)
                    t_writer.Write(New Long)
                    Dim r_name = Path.GetFileNameWithoutExtension(_group(pnt_id).table_entry_name) + ".indices"
                    n = System.Text.Encoding.Default.GetBytes(r_name)
                    t_writer.Write(CInt(n.Length))
                    t_writer.Write(n)
                    l = (t_writer.BaseStream.Position) Mod 4L
                    'writing padding bytes if needed
                    If l > 0 Then
                        For i = 1 To 4 - l
                            t_writer.Write(b)
                        Next
                    End If

                    sect_start = br.BaseStream.Position

                    Dim h1() = "BPVTxyznuviiiwwtb".ToArray
                    Dim h2() = "set3/xyznuviiiwwtbpc".ToArray
                    ReDim Preserve h1(67)
                    ReDim Preserve h2(63)
                    br.Write(h1)
                    br.Write(h2)
                    'write primitive count
                    br.Write(_group(pnt_id).nVertices_)
                    For i = 0 To _group(pnt_id).nVertices_ - 1
                        If frmWritePrimitive.hide_tracks_cb.Checked Then
                            br.Write(0.0!)
                            br.Write(0.0!)
                            br.Write(0.0!)
                        Else
                            br.Write(_group(pnt_id).vertices(i).x)
                            br.Write(_group(pnt_id).vertices(i).y)
                            br.Write(_group(pnt_id).vertices(i).z)
                        End If

                        br.Write(_group(pnt_id).vertices(i).n)
                        br.Write(_group(pnt_id).vertices(i).u)
                        br.Write(_group(pnt_id).vertices(i).v)
                        If False Then ' fills vertex color and weight to stop deforming of mesh.
                            Dim b0 As Byte = 0
                            Dim b255 As Byte = 0
                            br.Write(b0)
                            br.Write(b0)
                            br.Write(b0)
                            br.Write(b0)

                            br.Write(b0)
                            br.Write(b0)
                            br.Write(b255)
                            br.Write(b0)
                        Else

                            br.Write(_group(pnt_id).vertices(i).index_1)
                            br.Write(_group(pnt_id).vertices(i).index_2)
                            br.Write(_group(pnt_id).vertices(i).index_3)
                            br.Write(_group(pnt_id).vertices(i).index_4)

                            br.Write(_group(pnt_id).vertices(i).weight_1)
                            br.Write(_group(pnt_id).vertices(i).weight_2)
                            br.Write(_group(pnt_id).vertices(i).weight_3)
                            br.Write(_group(pnt_id).vertices(i).weight_4)
                        End If

                        br.Write(_group(pnt_id).vertices(i).t)
                        br.Write(_group(pnt_id).vertices(i).bn)


                    Next
                    p = br.BaseStream.Position - sect_start
                    l = (br.BaseStream.Position) Mod 4L
                    Dim padding2 = CUInt(l)
                    'writing padding bytes if needed
                    If l > 0 Then
                        For i = 1 To 4 - l
                            br.Write(b)
                        Next
                    End If
                    'second entry in table at end ==================================================
                    'write entry in table
                    t_writer.Write(CUInt(p))
                    t_writer.Write(New Long)
                    t_writer.Write(New Long)
                    r_name = Path.GetFileNameWithoutExtension(_group(pnt_id).table_entry_name) + ".vertices"
                    n = System.Text.Encoding.Default.GetBytes(r_name)
                    t_writer.Write(CInt(n.Length))
                    t_writer.Write(n)
                    l = (t_writer.BaseStream.Position) Mod 4L
                    'writing padding bytes if needed
                    If l > 0 Then
                        For i = 1 To 4 - l
                            t_writer.Write(b)
                        Next
                    End If
                    sect_start = br.BaseStream.Position
                    'write uv2s
                    'write UV2 section header
                    Dim h3() = "BPVSuv2".ToArray
                    Dim h4() = "set3/uv2pc".ToArray
                    ReDim Preserve h3(67)
                    ReDim Preserve h4(63)
                    br.Write(h3)
                    br.Write(h4)
                    'write count
                    br.Write(_group(pnt_id).nVertices_)
                    'write UV2s
                    For i = 0 To _group(pnt_id).nVertices_ - 1
                        br.Write(_group(pnt_id).vertices(i).u2)
                        br.Write(_group(pnt_id).vertices(i).v2)
                    Next
                    'third entry in table at end ==================================================
                    'write entry in table
                    p = br.BaseStream.Position - sect_start
                    t_writer.Write(CUInt(p))
                    t_writer.Write(New Long)
                    t_writer.Write(New Long)
                    r_name = Path.GetFileNameWithoutExtension(_group(pnt_id).table_entry_name) + ".uv2"
                    n = System.Text.Encoding.Default.GetBytes(r_name)
                    t_writer.Write(CInt(n.Length))
                    t_writer.Write(n)
                    l = (t_writer.BaseStream.Position) Mod 4L
                    If l > 0 Then
                        For i = 1 To 4 - l
                            t_writer.Write(b)
                        Next
                    End If
                    'section += 1
                End If
                '================================================================ CARRAIGE

                If _group(pnt_id).table_entry_name.ToLower.Contains("chass") And
                    _group(pnt_id).table_entry_name = section_names(1).names(k) Then
                    'save current position
                    '-------------------------------------------------------------
                    frmMain.info_Label.Text = "Compacting Data ID=" + pnt_id.ToString
                    Application.DoEvents()
                    Dim comp As comp_ = compact_primitive(pnt_id, fbxgrp(pnt_id).comp)
                    '-------------------------------------------------------------
                    Dim sect_start = br.BaseStream.Position
                    'assuming there will never be a "list2" in these files
                    Dim n = System.Text.Encoding.Default.GetBytes("list")
                    ReDim Preserve n(63) 'pad by resizing
                    br.Write(n) ' save string as padded binary
                    br.Write(comp.indi_cnt)
                    br.Write(1) ' write group count
                    'write indices
                    Dim c As Integer = 0
                    Try
                        For i = 0 To comp.indi_cnt - 1 Step 3
                            c = i + 2
                            If Not frmWritePrimitive.flipWindingOrder_cb.Checked Then
                                br.Write(Convert.ToUInt16(comp.indices(i + 1)))
                                br.Write(Convert.ToUInt16(comp.indices(i + 0)))
                                br.Write(Convert.ToUInt16(comp.indices(i + 2)))
                            Else
                                br.Write(Convert.ToUInt16(comp.indices(i + 0)))
                                br.Write(Convert.ToUInt16(comp.indices(i + 1)))
                                br.Write(Convert.ToUInt16(comp.indices(i + 2)))
                            End If
                        Next
                    Catch ex As Exception
                        MsgBox("failed writing indices OBJ:" + _group(pnt_id).name, MsgBoxStyle.Exclamation, "Fail!")
                        ms_table.Close()
                        t_writer.Close()
                        t_writer.Dispose()
                        r.Close()
                        br.Close()
                        br.Dispose()
                        Return

                    End Try

                    'write entry for each model in this group
                    br.Write(CInt(0)) ' indices start index
                    br.Write(CUInt(comp.nPrimitives)) ' primitive count
                    br.Write(CInt(0)) ' vertices start index
                    br.Write(CUInt(comp.vert_cnt))

                    p = br.BaseStream.Position - sect_start
                    l = (br.BaseStream.Position) Mod 4L
                    padding = CUInt(l)
                    'writing padding bytes if needed
                    If l > 0 Then
                        For i = 1 To 4 - l
                            br.Write(b)
                        Next
                    End If
                    'first entry in table at end ==================================================
                    t_writer.Write(CUInt(p))
                    t_writer.Write(New Long)
                    t_writer.Write(New Long)
                    Dim r_name = Path.GetFileNameWithoutExtension(_group(pnt_id).table_entry_name) + ".indices"
                    n = System.Text.Encoding.Default.GetBytes(r_name)
                    t_writer.Write(CInt(n.Length))
                    t_writer.Write(n)
                    l = (t_writer.BaseStream.Position) Mod 4L
                    If l > 0 Then
                        For i = 1 To 4 - l
                            t_writer.Write(b)
                        Next
                    End If

                    sect_start = br.BaseStream.Position

                    Dim h1() = "BPVTxyznuviiiwwtb".ToArray
                    Dim h2() = "set3/xyznuviiiwwtbpc".ToArray
                    ReDim Preserve h1(67)
                    ReDim Preserve h2(63)
                    br.Write(h1)
                    br.Write(h2)
                    'write primitive count
                    br.Write(comp.vert_cnt)
                    Try

                        For i = 0 To comp.vert_cnt - 1
                            br.Write(comp.vertices(i).x)
                            br.Write(comp.vertices(i).y)
                            br.Write(comp.vertices(i).z)

                            br.Write(comp.vertices(i).n)
                            br.Write(comp.vertices(i).u)
                            br.Write(comp.vertices(i).v)

                            br.Write(comp.vertices(i).index_1)
                            br.Write(comp.vertices(i).index_2)
                            br.Write(comp.vertices(i).index_3)

                            br.Write(CByte(0))

                            br.Write(CByte(0))
                            br.Write(CByte(0))
                            'br.Write(_group(pnt_id).vertices(i).weight_3)
                            br.Write(CByte(255))
                            br.Write(CByte(0))

                            br.Write(comp.vertices(i).t)
                            br.Write(comp.vertices(i).bn)


                        Next
                    Catch ex As Exception
                        MsgBox("failed writing vertices OBJ:" + _group(pnt_id).name, MsgBoxStyle.Exclamation, "Fail!")
                        ms_table.Close()
                        t_writer.Close()
                        t_writer.Dispose()
                        r.Close()
                        br.Close()
                        br.Dispose()
                        Return

                    End Try
                    p = br.BaseStream.Position - sect_start
                    l = (br.BaseStream.Position) Mod 4L
                    Dim padding2 = CUInt(l)
                    'writing padding bytes if needed
                    If l > 0 Then
                        For i = 1 To 4 - l
                            br.Write(b)
                        Next
                    End If
                    'second entry in table at end ==================================================
                    'write entry in table
                    t_writer.Write(CUInt(p))
                    t_writer.Write(New Long)
                    t_writer.Write(New Long)
                    r_name = Path.GetFileNameWithoutExtension(_group(pnt_id).table_entry_name) + ".vertices"
                    n = System.Text.Encoding.Default.GetBytes(r_name)
                    t_writer.Write(CInt(n.Length))
                    t_writer.Write(n)
                    l = (t_writer.BaseStream.Position) Mod 4L
                    If l > 0 Then
                        For i = 1 To 4 - l
                            t_writer.Write(b)
                        Next
                    End If
                End If

            Next
        Next ' section names loop
        table_size = t_writer.BaseStream.Position - 1
        ReDim Preserve table(table_size)
        br.Write(table)
        br.Write(table.Length)

        'File.WriteAllBytes("C:\test_table.bin", table) 'write debuging file

        'clean up stuff
        ms_table.Close()
        t_writer.Close()
        t_writer.Dispose()
        r.Close()
        br.Close()
        br.Dispose()

        edit_visual()


    End Sub
    Private Sub edit_visual()
        Dim visual_changed As Boolean = False
        Dim xml = XML_Strings(1).Replace("  ", "")
        xml = xml.Replace(vbCrLf, vbLf)
        Dim xmlArray = xml.Split(vbLf)

        If visual_changed Then ' write it if it has changed

            Dim path = My.Settings.res_mods_path
            Dim a = m_groups(1).f_name(0).ToLower.Split("lod0")
            Dim p = path + "\" + a(0) + "lod0\Chassis.visual_processed"
            Dim out_file As String = xmlArray(0)
            For i = 1 To xmlArray.Length - 1
                out_file += vbLf
                out_file += xmlArray(i)
            Next
            For i = 0 To 100
                out_file = out_file.Replace("<primitiveGroup>" + vbLf + "<PG_ID>" + i.ToString + "</PG_ID>", "<primitiveGroup>" + i.ToString)
            Next

            out_file = out_file.Replace("SceneRoot", "Scene Root")

            File.WriteAllText(p, out_file.Replace("map_", "chassis.visual_processed"))
        End If
    End Sub

    Private Sub update_chassis_visual_one(ByRef data() As String, ByVal m() As Double, ByVal idx As Integer)
        Dim r0, r1, r2, r3 As vec3

        r0.x = Math.Round(m(0), 6)
        r0.y = Math.Round(m(1), 6)
        r0.z = Math.Round(m(2), 6)

        r1.x = Math.Round(m(4), 6)
        r1.y = Math.Round(m(5), 6)
        r1.z = Math.Round(m(6), 6)

        r2.x = Math.Round(m(8), 6)
        r2.y = Math.Round(m(9), 6)
        r2.z = -Math.Round(m(10), 6)

        r3.x = Math.Round(m(12), 6)
        r3.y = -Math.Round(m(13), 6)
        r3.z = -Math.Round(m(14), 6)
        data(idx + 0) = make_entry_string("row0", r0)
        data(idx + 1) = make_entry_string("row1", r1)
        data(idx + 2) = make_entry_string("row2", r2)
        data(idx + 3) = make_entry_string("row3", r3)
        Return
    End Sub
    Private Sub update_chassis_visual_two(ByRef data() As String, ByVal m() As Double, ByVal idx As Integer)
        Dim r0, r1, r2, r3 As vec3

        r0.x = Math.Round(m(0), 6)
        r0.y = Math.Round(m(1), 6)
        r0.z = Math.Round(m(2), 6)

        r1.x = Math.Round(m(4), 6)
        r1.y = Math.Round(m(5), 6)
        r1.z = Math.Round(m(6), 6)

        r2.x = Math.Round(m(8), 6)
        r2.y = Math.Round(m(9), 6)
        r2.z = Math.Round(m(10), 6)

        r3.x = -Math.Round(m(12), 6)
        r3.y = Math.Round(m(13), 6)
        r3.z = Math.Round(m(14), 6)
        data(idx + 0) = make_entry_string("row0", r0)
        data(idx + 1) = make_entry_string("row1", r1)
        data(idx + 2) = make_entry_string("row2", r2)
        data(idx + 3) = make_entry_string("row3", r3)
        Return
    End Sub
    Private Sub update_chassis_visual_three(ByRef data() As String, ByVal m() As Double, ByVal idx As Integer)
        Dim r0, r1, r2, r3 As vec3

        r0.x = Math.Round(m(0), 6)
        r0.y = Math.Round(m(1), 6)
        r0.z = Math.Round(m(2), 6)

        r1.x = Math.Round(m(4), 6)
        r1.y = Math.Round(m(5), 6)
        r1.z = Math.Round(m(6), 6)

        r2.x = Math.Round(m(8), 6)
        r2.y = Math.Round(m(9), 6)
        r2.z = -Math.Round(m(10), 6)

        r3.x = Math.Round(m(12), 6)
        r3.y = Math.Round(m(13), 6)
        r3.z = -Math.Round(m(14), 6)
        Dim v = get_vec3_from_string(data(idx + 3))
        Dim diff = v.y + r3.y
        Dim diff2 = v.z - r3.z

        v.y += diff
        'v.x = r3.x
        'v.z += diff2
        data(idx + 0) = make_entry_string("row0", r0)
        data(idx + 1) = make_entry_string("row1", r1)
        data(idx + 2) = make_entry_string("row2", r2)
        data(idx + 3) = make_entry_string("row3", v)
        Return
    End Sub
    Private Sub update_chassis_visual_four(ByRef data() As String, ByVal m() As Double, ByVal idx As Integer)
        Dim r0, r1, r2, r3 As vec3

        r0.x = Math.Round(m(0), 6)
        r0.y = Math.Round(m(1), 6)
        r0.z = Math.Round(m(2), 6)

        r1.x = Math.Round(m(4), 6)
        r1.y = Math.Round(m(5), 6)
        r1.z = Math.Round(m(6), 6)

        r2.x = Math.Round(m(8), 6)
        r2.y = Math.Round(m(9), 6)
        r2.z = Math.Round(m(10), 6)

        r3.x = -Math.Round(m(12), 6)
        r3.y = Math.Round(m(13), 6)
        r3.z = Math.Round(m(14), 6)
        Dim v = get_vec3_from_string(data(idx + 3))
        Dim diff = v.y + r3.y
        'Dim diff2 = v.z - r3.z

        v.x = r3.x
        v.y += diff
        v.z = r3.z
        data(idx + 0) = make_entry_string("row0", r0)
        data(idx + 1) = make_entry_string("row1", r1)
        data(idx + 2) = make_entry_string("row2", r2)
        data(idx + 3) = make_entry_string("row3", v)
        Return
    End Sub
    Private Function make_entry_string(ByVal row As String, ByVal v As vec3) As String
        Dim rs As String =
            "<" + row + ">" + v.x.ToString("0.000000") + " " + v.y.ToString("0.000000") + " " + v.z.ToString("0.000000") +
            "</" + row + ">"
        Return rs
    End Function
    Private Function get_vr_matrix(ByRef da() As String, ByVal idx As Integer) As mat_
        Dim mat As New mat_
        Dim v1 = get_vec3_from_string(da(idx + 0))
        Dim v2 = get_vec3_from_string(da(idx + 1))
        Dim v3 = get_vec3_from_string(da(idx + 2))
        Dim v4 = get_vec3_from_string(da(idx + 3))
        ReDim mat.mat(15)
        mat.mat(0) = v1.x
        mat.mat(1) = v1.y
        mat.mat(2) = v1.z
        mat.mat(3) = 0.0

        mat.mat(4) = v2.x
        mat.mat(5) = v2.y
        mat.mat(6) = v2.z
        mat.mat(7) = 0.0

        mat.mat(8) = v3.x
        mat.mat(9) = v3.y
        mat.mat(10) = -v3.z
        mat.mat(11) = 0.0

        mat.mat(12) = v4.x
        mat.mat(13) = -v4.y
        mat.mat(14) = -v4.z
        mat.mat(15) = 1.0


        Return mat
    End Function
    Private Function get_vec3_from_string(ByVal s As String) As vec3
        Dim ss = trim_string(s)
        Dim a = ss.Split(" ")
        Dim v As New vec3
        v.x = Convert.ToSingle(a(0))
        v.y = Convert.ToSingle(a(1))
        v.z = Convert.ToSingle(a(2))
        Return v
    End Function
    Private Function trim_string(ByVal s As String)
        Dim a = s.Split(">")
        Dim b = a(1).Split("<")
        s = b(0).Trim
        Return s
    End Function

    Private Function remove_string_entry(ByRef data() As String, ByVal str As String) As Boolean
        Dim start As Integer = 0
        Dim restart As Integer = 0

next_section:
        For k = restart To data.Length - 1
            start = k
            If data(k).ToLower.Contains("<renderset>") Then
                GoTo found_section
            End If
        Next
        Return False

found_section:
        For i = start To data.Length - 1
            If str = data(i) Then
                data(i) = ""
                Return True
            End If
            restart = i
        Next
        GoTo next_section

    End Function



    Public Sub write_primitives(ByVal ID As Integer)
        updateEvent.Reset()


        Dim tsa() As Char
        Dim dummy As UInt32 = 0
        Dim i As UInt32
        Dim indi_size, vert_size, UV2_size As UInt32
        Dim r As FileStream = Nothing
        uv2_total_count = 0
        ReDim fbx_uv2s(0)
        ReDim fbx_uv2s(1000000)
        obj_cnt = m_groups(ID).cnt
        Try
            r = New FileStream(My.Settings.res_mods_path + "\" + m_groups(ID).f_name(0).Replace(".model", ".primitives_processed"), FileMode.OpenOrCreate, FileAccess.Write)
        Catch e As Exception
            MsgBox("I could not open """ + My.Settings.res_mods_path + "\" + m_groups(ID).f_name(0) + """!" + vbCrLf +
                    "The Root folder is there but there are no  .primitive_processed files." + vbCrLf _
                    + " Did you delete them?", MsgBoxStyle.Exclamation, "Can find folder!")
            Return
        End Try

        br = New BinaryWriter(r)
        Dim mh As UInt32 = &H42A14E65
        br.Write(mh) ' write magic number

        Dim p = r.Position
        write_list_data(ID) ' write indices list and indexing table
        indi_size = r.Position - p ' get section size
        indi_size -= Ipadding ' l is the padding amount written
        '-------------------------------------------------------------

        p = r.Position
        write_vertex_data(ID) 'write out vertices and UV2s if they exist
        vert_size = r.Position - p ' get section size
        vert_size -= Vpadding ' l is the padding amount written
        '-------------------------------------------------------------
        ReDim Preserve fbx_uv2s(uv2_total_count - 1)
        '-------------------------------------------------------------

        p = r.Position
        write_UV2(ID) 'write out vertices and UV2s if they exist
        UV2_size = r.Position - p ' get section size
        UV2_size -= UV2padding ' l is the padding amount written
        '-------------------------------------------------------------

        'write colored vertice data if the model has them.
        'Color is NEVER put back in the primtive OR Visual file!

        '-------------------------------------------------------------

        Dim header_length As UInt32 = 68 + 64

        Dim offset As UInt32 = 0
        '##### Write table containing the sizes and names of the sections in the file
        '-------------------------------------------------------------
        'write indices table entry
        tsa = "indices".ToArray
        If ID = 4 Then
            For i = 0 To section_names(ID).names.Length - 1
                If section_names(ID).names(i).ToLower.Contains("indices") Then
                    tsa = section_names(ID).names(i).ToArray

                End If
            Next
        End If

        br.Write(indi_size) ' size of data
        br.Write(dummy) : br.Write(dummy) : br.Write(dummy) : br.Write(dummy) ' fill data
        br.Write(Convert.ToUInt32(tsa.Length)) ' string length
        br.Write(tsa) ' string
        offset += tsa.Length + 24 ' adjust table start by this entry length
        l = (br.BaseStream.Position) Mod 4L
        Debug.WriteLine("ind_pnt" + vbTab + "base {0} , mod-4 {1} " + vbCrLf, br.BaseStream.Position, (br.BaseStream.Position) Mod 4L)
        If l > 0 Then ' pad to int aligmenment
            For i = 1 To 4 - l
                br.Write(b)
            Next
            offset += i - 1
        End If
        '-------------------------------------------------------------
        'write vertice table entry
        Dim tvn As String = ""
        tsa = "vertices".ToArray ' convert to char array
        If ID = 4 Then
            For i = 0 To section_names(ID).names.Length - 1
                If section_names(ID).names(i).ToLower.Contains("vertices") Then
                    tsa = section_names(ID).names(i).ToArray
                End If
            Next
        End If
        br.Write(vert_size)
        br.Write(dummy) : br.Write(dummy) : br.Write(dummy) : br.Write(dummy) ' padding
        br.Write(Convert.ToUInt32(tsa.Length))
        br.Write(tsa)
        offset += tsa.Length + 24
        l = (br.BaseStream.Position) Mod 4L
        Debug.WriteLine("vts_pnt" + vbTab + "base {0} , mod-4 {1} " + vbCrLf, br.BaseStream.Position, (br.BaseStream.Position) Mod 4L)
        If l > 0 Then
            For i = 1 To 4 - l
                br.Write(b)
            Next
            offset += i - 1
        End If        '-------------------------------------------------------------
        If save_has_uv2 And PRIMITIVES_MODE Then
            'write uv2 table entry
            tsa = "uv2".ToArray
            br.Write(UV2_size)
            br.Write(dummy) : br.Write(dummy) : br.Write(dummy) : br.Write(dummy)
            br.Write(Convert.ToUInt32(tsa.Length))
            br.Write(tsa)
            offset += tsa.Length + 24
            l = (br.BaseStream.Position) Mod 4L
            Console.Write("uv2_pnt" + vbTab + "base {0} , mod-4 {1} " + vbCrLf, br.BaseStream.Position, (br.BaseStream.Position) Mod 4L)
            If l > 0 Then
                For i = 1 To 4 - l
                    br.Write(b)
                Next
                offset += i - 1
            End If
        End If
no_UV2EVER:
        '-------------------------------------------------------------

        br.Write(offset) ' start of entries at the end of the file.

        'thats all folks !!
        br.Close()
        r.Close()
        r.Dispose()
        r = Nothing
        Dim f = XML_Strings(m_groups(ID).m_type)
        f = f.Replace(vbCr, "")
        Dim pos As Integer = 0
        OBJECT_WAS_INSERTED = m_groups(ID).new_objects

        If OBJECT_WAS_INSERTED Then


            '------------------------

            Dim inst_start As Integer = 0
            Dim pgrp As Integer = 0
            For pos = 0 To m_groups(ID).cnt
                If f.Contains("<PG_ID>" + pos.ToString + "</PG_ID>") Then
                    inst_start = InStr(f, "<PG_ID>" + pos.ToString + "</PG_ID>")
                    pgrp += 1
                End If
            Next

            pos = 0
            'Dim templateColorOnly As String
            'Dim templateNormal As String
            'Dim templateNormalSpec As String
            Dim templatePBR As String = ""
            If ID = 4 Then
                templatePBR = File.ReadAllText(Application.StartupPath + "\Templates\templatePBR_skinned.txt")
            Else
                templatePBR = File.ReadAllText(Application.StartupPath + "\Templates\templatePBR.txt")
            End If
            Dim first, last As Integer
            first = m_groups(ID).existingCount
            last = m_groups(ID).cnt - first
            For item_num = first To last
                Dim fbx_id As Integer = m_groups(ID).list(item_num) 'get id for this new item
                Dim new_name = fbxgrp(fbx_id).name ' get objects name
                Dim primObj As String = ""


                primObj = templatePBR

                'check for legit texture assignments
                If fbxgrp(fbx_id).normal_name Is Nothing And fbxgrp(fbx_id).color_name IsNot Nothing Then
                    fbxgrp(fbx_id).normal_name = "REPLACE_ME_WITH_TEXTURE_PATH"
                End If
                primObj = primObj.Replace("<PG_ID>0</PG_ID>", "<PG_ID>" + pgrp.ToString + "</PG_ID>") ' update primitive grp id
                pgrp += 1 ' add one for each new item
                primObj = primObj.Replace("Kustom_mat", new_name) ' update indentity name

                Try ' this will change shortly
                    Dim new_s As String = fbxgrp(fbx_id).normal_name
                    primObj = primObj.Replace("NORMAL_NAME", move_convert_new_textures(new_s, fbxgrp(fbx_id).name)) ' update normal texture name
                Catch ex As Exception
                End Try

                Try
                    Dim new_s As String = fbxgrp(fbx_id).color_name
                    primObj = primObj.Replace("COLOR_NAME", move_convert_new_textures(new_s, fbxgrp(fbx_id).name)) ' update diffuse texture name
                Catch ex As Exception
                End Try

                Try
                    Dim new_s As String = Application.StartupPath + "\templates\dummy_GMM.png"
                    primObj = primObj.Replace("GMM_NAME", move_convert_new_textures(new_s, fbxgrp(fbx_id).name)) ' update GMM texture name
                Catch ex As Exception
                End Try
                Try
                    Dim new_s As String = Application.StartupPath + "\templates\dummy_AO.png"
                    primObj = primObj.Replace("AO_NAME", move_convert_new_textures(new_s, fbxgrp(fbx_id).name)) ' update AO texture name
                Catch ex As Exception
                End Try

                Try
                    Dim new_s As String = Application.StartupPath + "\templates\dummy_ID.png"
                    primObj = primObj.Replace("COLOR_ID_NAME", move_convert_new_textures(new_s, fbxgrp(fbx_id).name)) ' update ID texture name
                Catch ex As Exception
                End Try

                pos = f.IndexOf("<groupOrigin>", inst_start)
                inst_start = pos
                f = f.Insert(pos, primObj)
            Next


        End If

        f = f.Replace("  ", "")
        f = f.Replace(vbCrLf, vbLf)
        Dim va = f.Split(vbLf)
        Dim v2(va.Length - 1) As String
        Dim cnt As Integer = 0
        For i = 0 To va.Length - 1
            If Not va(i).Contains("colour") Then
                v2(cnt) = va(i)
                cnt += 1
            End If
        Next
        f = ""
        For i = 0 To cnt - 1
            f += v2(i) + vbLf
        Next
        f = f.Replace(vbTab, "")
        For i = 0 To 100
            f = f.Replace("<primitiveGroup>" + vbLf + "<PG_ID>" + i.ToString + "</PG_ID>", "<primitiveGroup>" + i.ToString)
        Next

        f = f.Replace("SceneRoot", "Scene Root")
        Dim fn As String = m_groups(ID).f_name(0)
        fn = fn.Replace(".primitives", ".visual")
        File.WriteAllText(My.Settings.res_mods_path + "\" + fn.Replace(".model", ".visual_processed"), f)

        updateEvent.Set()

    End Sub

    Private Function move_convert_new_textures(ByVal path_ As String, ByVal t_s As String) As String
        Dim new_path As String = ""
        If path_ Is Nothing Then Return Nothing

        Dim t_type As String = ""
        If t_s.ToLower.Contains("chassis") Then
            t_type = "chassis"
        End If
        If t_s.ToLower.Contains("hull") Then
            t_type = "hull"
        End If
        If t_s.ToLower.Contains("turret") Then
            t_type = "turret"
        End If
        If t_s.ToLower.Contains("gun") Then
            t_type = "gun"
        End If
        Dim delim As String = "\normal"
        If CRASH_MODE Then delim = "\crash"
        Dim r_path = My.Settings.res_mods_path
        Dim res_path As String = My.Settings.res_mods_path + "\"
        Dim a = m_groups(1).f_name(0).Split(delim)
        Dim p = a(0)
        Dim status As Boolean
        Dim pat As String
        Try

            new_path = IO.Path.GetFileNameWithoutExtension(path_)
            Dim subf = Path.GetFileName(TANK_NAME)
            Dim folder = Path.GetDirectoryName(frmMain.OpenFileDialog1.FileName) + "\" + subf
            Dim compete_path = folder + "\" + Path.GetFileName(path_)
            If File.Exists(res_path + p + "\" + new_path + ".DDS") Then
                pat = p + "\" + new_path + ".DDS"
                Return pat.Replace("\", "/")
            End If
            Dim id = Il.ilGenImage
            Il.ilBindImage(id)
            Il.ilLoadImage(compete_path)
            Ilu.iluBuildMipmaps()
            If Not new_path.ToLower.Contains(t_type) Then
                pat = p + "\" + t_type + "_" + new_path + ".DDS"
            Else
                pat = p + "\" + new_path + ".DDS"
            End If
            pat = pat.Replace("\", "/")
            'If File.Exists(res_path + pat) Then Return pat.Replace("\", "/")

            Il.ilSetInteger(Il.IL_IMAGE_FORMAT, Il.IL_DXT1)
            Select Case True
                Case path_.Contains("ANM.")
                    Il.ilSetInteger(Il.IL_IMAGE_FORMAT, Il.IL_DXT5)
                    Exit Select
                Case path_.ToLower.Contains("normal")
                    Il.ilSetInteger(Il.IL_IMAGE_FORMAT, Il.IL_DXT5)
                    Exit Select
                Case path_.ToLower.Contains("AM.")
                    Il.ilSetInteger(Il.IL_IMAGE_FORMAT, Il.IL_DXT1)
                    Exit Select
                Case path_.ToLower.Contains("ao.")
                    Il.ilSetInteger(Il.IL_IMAGE_FORMAT, Il.IL_DXT1)
                    Exit Select
                Case path_.ToLower.Contains("gmm.")
                    Il.ilSetInteger(Il.IL_IMAGE_FORMAT, Il.IL_DXT1)
                    Exit Select

            End Select

            Il.ilEnable(Il.IL_FILE_OVERWRITE)

            status = Il.ilSave(Il.IL_DDS, res_path + pat)

            Il.ilBindImage(0)
            Il.ilDeleteImage(id)


            If Not status Then
                MsgBox("Could not write " + res_path + new_path, MsgBoxStyle.Exclamation, "Oh No!!")
                Return path_
            End If

        Catch ex As Exception
            MsgBox("Could not write " + res_path + new_path, MsgBoxStyle.Exclamation, "Oh No!!")
            Return path_
        End Try
        Return pat.Replace("\", "/")
    End Function

    Private Sub write_vertex_data(ByVal id As Integer)
        Dim j As Integer
        Dim h() = "                         ".ToArray
        stride = fbxgrp(id).stride
        If stride = 40 Then
            Dim h1() = "xyznuvtb".ToArray
            h = h1
        Else
            Dim h1() = "xyznuviiiwwtb".ToArray
            h = h1
        End If
        h = "BPVTxyznuvtb".ToArray
        Dim h2 = "set3/xyznuvtbpc".ToArray
        If id = 4 Then
            h2 = "set3/xyznuviiiwwtbpc".ToArray
            h = "BPVTxyznuviiiwwtb".ToArray
        End If
        ReDim Preserve h(67)
        ReDim Preserve h2(63)
        br.Write(h)
        br.Write(h2)
        'Dim total_verts As UInt32
        Dim obj_cnt = m_groups(id).cnt
        Dim pnter As Integer
        total_verts = 0

        For i = 1 To obj_cnt
            pnter = m_groups(id).list(i - 1)
            total_verts += fbxgrp(pnter).comp.vert_cnt
        Next
        Dim idx_size As Integer = 2 '############## this will need to change If the total indice count is > FFFF (65535)
        Dim indi_cnt As Integer = 0
        For i = 1 To obj_cnt
            pnter = m_groups(id).list(i - 1)
            indi_cnt += fbxgrp(pnter).comp.indi_cnt
        Next
        Dim parent = m_groups(id).list(0)
        br.Write(total_verts)
        For i = 1 To obj_cnt

            create_TBNS2(i)

            pnter = m_groups(id).list(i - 1)
            '-------------------------------------------------------------
            Application.DoEvents()
            Dim comp As comp_ = fbxgrp(pnter).comp
            '-------------------------------------------------------------
            j = comp.vert_cnt - 1
            For k = 0 To j
                'r.Close()
                'Return
                Dim n As vect3
                If fbxgrp(pnter).is_new_model Then
                    Dim v As New vect3
                    v.x = comp.vertices(k).x
                    v.y = comp.vertices(k).y
                    v.z = comp.vertices(k).z
                    If id = 4 Then
                        v = gun_new_transform(v, fbxgrp(pnter).matrix)
                        v.x -= fbxgrp(parent).matrix(12)
                        v.y -= fbxgrp(parent).matrix(13)
                        v.z += fbxgrp(parent).matrix(14)
                    Else
                        v = transform(v, fbxgrp(pnter).matrix)
                        v.x -= fbxgrp(parent).matrix(12)
                        v.y -= fbxgrp(parent).matrix(13)
                        v.z -= fbxgrp(parent).matrix(14)

                    End If

                    br.Write(v.x)
                    br.Write(v.y)
                    br.Write(v.z)

                Else
                    If id = 3333 Then 'set to 3 to hide the turret, 2 for hull and so on.
                        br.Write(0.0!)
                        br.Write(0.0!)
                        br.Write(0.0!)
                    Else
                        br.Write(comp.vertices(k).x)
                        br.Write(comp.vertices(k).y)
                        br.Write(comp.vertices(k).z)

                    End If
                End If

                'sucks but we have to transform N, T and Bt
                n.x = comp.vertices(k).nx
                n.y = comp.vertices(k).ny
                n.z = comp.vertices(k).nz
                Dim tn As vect3
                tn.x = comp.vertices(k).tx
                tn.y = comp.vertices(k).ty
                tn.z = comp.vertices(k).tz

                Dim tb As vect3
                tb.x = comp.vertices(k).bnx
                tb.y = comp.vertices(k).bny
                tb.z = comp.vertices(k).bnz




                comp.vertices(k).n = packnormalFBX888_writePrimitive(toFBXv(n))
                comp.vertices(k).t = packnormalFBX888_writePrimitive(toFBXv(tn))
                comp.vertices(k).bn = packnormalFBX888_writePrimitive(toFBXv(tb))
                'n = rotate_transform(n, fbxgrp(pnter).matrix)

                br.Write(comp.vertices(k).n)
                br.Write(comp.vertices(k).u)
                br.Write(comp.vertices(k).v)
                If stride = 37 Then
                    br.Write(comp.vertices(k).index_1)
                    br.Write(comp.vertices(k).index_2)
                    br.Write(comp.vertices(k).index_3)
                    br.Write(comp.vertices(k).weight_1)
                    br.Write(comp.vertices(k).weight_2)
                End If
                If id = 4 Then
                    br.Write(comp.vertices(k).index_1)
                    br.Write(comp.vertices(k).index_2)
                    br.Write(comp.vertices(k).index_3)
                    br.Write(CByte(0))

                    br.Write(comp.vertices(k).weight_1)
                    br.Write(comp.vertices(k).weight_2)
                    br.Write(CByte(255))
                    br.Write(CByte(0))

                End If
                br.Write(comp.vertices(k).t)
                br.Write(comp.vertices(k).bn)
            Next
        Next
        Dim l As Long = (br.BaseStream.Position) Mod 4L
        Vpadding = l
        Debug.WriteLine("vt raw" + vbTab + "base {0} , mod-4 {1} " + vbCrLf, br.BaseStream.Position, (br.BaseStream.Position) Mod 4L)
        If l > 0 Then
            For i = 1 To 4 - l
                br.Write(b)
            Next
        End If
    End Sub
    Private Sub write_UV2(id)
        '       save_has_uv2 = False


        Dim uv_cnt As UInteger = 0
        For i = 1 To obj_cnt
            pnter = m_groups(id).list(i - 1)
            If fbxgrp(pnter).has_uv2 = 1 Then
                uv_cnt += fbxgrp(pnter).comp.vert_cnt
            End If

        Next
        If uv_cnt = 0 Then
            save_has_uv2 = False
            Return
        End If
        Dim tbuf((uv_cnt * 8UI) + 136) As Byte
        Dim ms As New MemoryStream(tbuf)
        Dim tbr As New BinaryWriter(ms)

        Dim c_p1 = "BPVSuv2".ToCharArray
        ReDim Preserve c_p1(67)
        Dim c_p2 = "set3/uv2pc".ToCharArray
        ReDim Preserve c_p2(63)
        br.Write(c_p1)
        br.Write(c_p2)
        tbr.Write(c_p1)
        tbr.Write(c_p2)

        br.Write(uv_cnt)
        tbr.Write(uv_cnt)

        Dim rc As Integer = 0
        Try

            For i = 1 To obj_cnt
                pnter = m_groups(id).list(i - 1)
                If fbxgrp(pnter).has_uv2 = 1 Then
                    Dim comp As comp_ = fbxgrp(pnter).comp

                    For j = 0 To comp.vert_cnt - 1
                        'p = fbxgrp(pnter).indices(j).
                        br.Write(comp.vertices(j).u2)
                        br.Write(comp.vertices(j).v2)
                        tbr.Write(comp.vertices(j).u2)
                        tbr.Write(comp.vertices(j).v2)
                        rc += 8
                    Next
                    save_has_uv2 = True
                    l = (br.BaseStream.Position) Mod 4L
                    UV2padding = l
                    Console.Write("uv2 raw" + vbTab + "base {0} , mod-4 {1} " + vbCrLf, br.BaseStream.Position, (br.BaseStream.Position) Mod 4L)
                    If l > 0 Then
                        For j = 1 To 4 - l
                            br.Write(b)
                        Next
                    End If
                End If
            Next
            File.WriteAllBytes(Temp_Storage + "\Mikes_Dump.bin", tbuf)
        Catch ex As Exception

        End Try
        tbr.Close()
        ms.Dispose()
        'Debug.WriteLine(rc.ToString)
    End Sub
    Private Structure xyznuvtb_
        Dim x, y, z As Single
        Dim n As UInt32
        Dim u, v As Single
        Dim t, b As UInt32
    End Structure
    Private Sub write_list_data(ByVal id As Integer)
        Dim xyz As New xyznuvtb_
        Dim len_vertex As UInt32 = Marshal.SizeOf(xyz)
        For i = 1 To obj_cnt
            pnter = m_groups(id).list(i - 1)
            '-------------------------------------------------------------
            frmMain.info_Label.Text = "Compacting Data ID=" + pnter.ToString
            Application.DoEvents()
            compact_primitive(pnter, fbxgrp(pnter).comp)
            '-------------------------------------------------------------

        Next
        total_indices = 0
        For i = 1 To obj_cnt
            pnter = m_groups(id).list(i - 1)
            total_indices += fbxgrp(pnter).comp.indi_cnt
        Next
        Dim h2() = "list".ToArray
        ind_scale = 2
        If total_indices > &HFFFF Then
            ind_scale = 4
            h2 = "list32".ToArray
        End If
        ReDim Preserve h2(63)
        br.Write(h2)
        br.Write(total_indices)
        br.Write(Convert.ToUInt32(obj_cnt)) 'write how many objects there are in this model
        Dim off As UInt32 = 0
        For i = 1 To obj_cnt
            Dim cnt = 0
            pnter = m_groups(id).list(i - 1)
            '-------------------------------------------------------------
            '-------------------------------------------------------------
            Dim comp As comp_ = fbxgrp(pnter).comp

            For j As UInt32 = 0 To comp.indi_cnt - 1 Step 3
                'note: my routine uses other rotation
                If fbxgrp(pnter).is_new_model Then
                    If ind_scale = 2 Then
                        If frmWritePrimitive.flipWindingOrder_cb.Checked And Not id = 4 Then
                            br.Write(Convert.ToUInt16(comp.indices(j + 0) + off))
                            br.Write(Convert.ToUInt16(comp.indices(j + 1) + off))
                            br.Write(Convert.ToUInt16(comp.indices(j + 2) + off))
                        Else
                            br.Write(Convert.ToUInt16(comp.indices(j + 1) + off))
                            br.Write(Convert.ToUInt16(comp.indices(j + 0) + off))
                            br.Write(Convert.ToUInt16(comp.indices(j + 2) + off))

                        End If
                        If comp.indices(j + 0) + off > cnt Then cnt = comp.indices(j + 0) + off
                        If comp.indices(j + 1) + off > cnt Then cnt = comp.indices(j + 1) + off
                        If comp.indices(j + 2) + off > cnt Then cnt = comp.indices(j + 2) + off
                    Else
                        If frmWritePrimitive.flipWindingOrder_cb.Checked Or fbxgrp(pnter).reverse_winding And Not id = 4 Then
                            br.Write(Convert.ToUInt32(comp.indices(j + 0) + off))
                            br.Write(Convert.ToUInt32(comp.indices(j + 1) + off))
                            br.Write(Convert.ToUInt32(comp.indices(j + 2) + off))
                        Else
                            br.Write(Convert.ToUInt32(comp.indices(j + 1) + off))
                            br.Write(Convert.ToUInt32(comp.indices(j + 0) + off))
                            br.Write(Convert.ToUInt32(comp.indices(j + 2) + off))

                        End If
                        If comp.indices(j + 0) + off > cnt Then cnt = comp.indices(j + 0) + off
                        If comp.indices(j + 1) + off > cnt Then cnt = comp.indices(j + 1) + off
                        If comp.indices(j + 2) + off > cnt Then cnt = comp.indices(j + 2) + off
                    End If
                Else
                    If ind_scale = 2 Then
                        If frmWritePrimitive.flipWindingOrder_cb.Checked Or fbxgrp(pnter).reverse_winding Or id = 4 Then
                            br.Write(Convert.ToUInt16(comp.indices(j + 1) + off))
                            br.Write(Convert.ToUInt16(comp.indices(j + 0) + off))
                            br.Write(Convert.ToUInt16(comp.indices(j + 2) + off))
                        Else
                            br.Write(Convert.ToUInt16(comp.indices(j + 0) + off))
                            br.Write(Convert.ToUInt16(comp.indices(j + 1) + off))
                            br.Write(Convert.ToUInt16(comp.indices(j + 2) + off))
                        End If
                        If comp.indices(j + 0) + off > cnt Then cnt = comp.indices(j + 0) + off
                        If comp.indices(j + 1) + off > cnt Then cnt = comp.indices(j + 1) + off
                        If comp.indices(j + 2) + off > cnt Then cnt = comp.indices(j + 2) + off
                    Else
                        If frmWritePrimitive.flipWindingOrder_cb.Checked Or fbxgrp(pnter).reverse_winding Or id = 4 Then
                            br.Write(Convert.ToUInt32(comp.indices(j + 1) + off))
                            br.Write(Convert.ToUInt32(comp.indices(j + 0) + off))
                            br.Write(Convert.ToUInt32(comp.indices(j + 2) + off))
                        Else
                            br.Write(Convert.ToUInt32(comp.indices(j + 0) + off))
                            br.Write(Convert.ToUInt32(comp.indices(j + 1) + off))
                            br.Write(Convert.ToUInt32(comp.indices(j + 2) + off))
                        End If
                        If comp.indices(j + 0) + off > cnt Then cnt = comp.indices(j + 0) + off
                        If comp.indices(j + 1) + off > cnt Then cnt = comp.indices(j + 1) + off
                        If comp.indices(j + 2) + off > cnt Then cnt = comp.indices(j + 2) + off
                    End If
                End If
            Next
            off += comp.vert_cnt
        Next
        Dim s_index, s_vertex As UInt32
        For i = 1 To obj_cnt
            pnter = m_groups(id).list(i - 1)
            Dim pnter2 = pnter
            If i > 1 Then
                pnter2 = m_groups(id).list(i - 2) 'we have to do it this way because added items wont be in order
                s_index += (fbxgrp(pnter2).comp.indi_cnt)
                s_vertex += fbxgrp(pnter2).comp.vert_cnt
            End If
            pnter = m_groups(id).list(i - 1)
            br.Write(s_index)
            br.Write(CInt(fbxgrp(pnter).comp.nPrimitives))
            br.Write(s_vertex)
            br.Write(fbxgrp(pnter).comp.vert_cnt)
        Next
        l = (br.BaseStream.Position) Mod 4L
        Ipadding = l
        Debug.WriteLine("indices" + vbTab + "base {0} , mod-4 {1} " + vbCrLf, br.BaseStream.Position, (br.BaseStream.Position) Mod 4L)
        If l > 0 Then
            For i = 1 To 4 - l
                br.Write(b)
            Next
        End If

    End Sub

    Private Sub write_BSP2(ByVal id As Integer)
        'write BSP2 data if it exists
        Dim p = br.BaseStream.Position
        For i = 1 To obj_cnt
            pnter = m_groups(id).list(i - 1)
            If _group(pnter).bsp2_id > 0 Then
                'bsp2_size = _group(pnter).bsp2_data.Length - 2
                br.Write(_group(pnter).bsp2_data, 0, _group(pnter).bsp2_data.Length - 2)
                l = (br.BaseStream.Position) Mod 4L
                Console.Write("bsp2" + vbTab + "base {0} , mod-4 {1} " + vbCrLf, br.BaseStream.Position, (br.BaseStream.Position) Mod 4L)
                bsp2_size = br.BaseStream.Position - p ' get size
                If l > 0 Then
                    For j = 1 To 4 - l
                        br.Write(b)
                    Next
                End If
                p = br.BaseStream.Position
                'bsp2_mat_size = _group(pnter).bsp2_material_data.Length - 2
                br.Write(_group(pnter).bsp2_material_data, 0, _group(pnter).bsp2_material_data.Length - 2)
                bsp2_mat_size = br.BaseStream.Position - p ' get size
                l = (br.BaseStream.Position) Mod 4L
                Console.Write("bsp2_m" + vbTab + "base {0} , mod-4 {1} " + vbCrLf, br.BaseStream.Position, (br.BaseStream.Position) Mod 4L)
                If l > 0 Then
                    For j = 1 To 4 - l
                        br.Write(b)
                    Next
                End If
                Exit For
            End If
        Next


    End Sub

End Module