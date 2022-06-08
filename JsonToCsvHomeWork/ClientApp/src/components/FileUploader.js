import React, { Component } from 'react';
import { v4 as uuidv4 } from 'uuid';
import './FileUploader.css';


export class FileUploader extends Component {
    static displayName = FileUploader.name;

    constructor(props) {
        super(props);
        this.state = {
            selectedFile: null,
            table: props.table
        }
        this.onFileUpload = this.onFileUpload.bind(this);
        this.removePreviousFile = this.removePreviousFile.bind(this);
    }

    onFileChange = event => {
        this.setState({
            selectedFile: event.target.files[0]
        });
    }

    async onFileUpload() {
        const formData = new FormData();
        //attemp to remove previous file from server
        if (sessionStorage.getItem('currentGuid') !== null) {
            await this.removePreviousFile();
        }

        sessionStorage.setItem('currentGuid', uuidv4());
        formData.append(
            "currentFile",
            this.state.selectedFile,
            this.state.selectedFile.name
        )
        
        await fetch('jsonconverter/upload-file',
            {
                method: 'POST',
                headers: {
                    'Accept': 'application/json',
                    'clientId': sessionStorage.getItem('currentGuid') 
                },
                body: formData
            }
        ).then(response => response.json())
            .then(data => {
                this.props.handleTableUpdate(data);
                console.log(data);
            })
    }

    async removePreviousFile() {
        await fetch('jsonconverter/remove-file',
            {
                method: 'POST',
                headers: {
                    'Accept': 'application/json',
                    'clientId': sessionStorage.getItem('currentGuid')
                }
            }
        )
    }

    fileData = () => {

        if (this.state.selectedFile)
        {
            return (
                <div>
                    <h2>File Details:</h2>
                    <p>File Name: {this.state.selectedFile.name}</p>
                    <p>File Type: {this.state.selectedFile.type}</p>
                </div>
            );
        }
        else
        {
            return (
                <div>
                    <br />
                    <h4>Choose before Pressing the Upload button</h4>
                </div>
            );
        }
    };

    render() {
        return (
            <div className='file-uploader'>
                <h3>
                    Upload Json File!
                </h3>
                <div className='upload-container'>
                    <input className='input-file' type="file" onChange={this.onFileChange} />
                    <button className='upload-button' onClick={this.onFileUpload}>
                        Upload!
                    </button>
                </div>
                {this.fileData()}
            </div>
        );
    }
}