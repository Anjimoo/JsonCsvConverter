import React, { Component } from 'react';
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
    }

    onFileChange = event => {
        this.setState({
            selectedFile: event.target.files[0]
        });
    }

    async onFileUpload() {
        const formData = new FormData();

        formData.append(
            "myFile",
            this.state.selectedFile,
            this.state.selectedFile.name
        )

        let response = await fetch('jsonconverter/upload-file',
            {
                method: 'POST',
                headers: {
                    'Accept': 'application/json',
                },
                body: formData
            }
        ).then(response => response.json())
            .then(data => {
                this.props.handleTableUpdate(data);
                console.log(data);
            })

        //TO DO request to backend api
    }

    fileData = () => {

        if (this.state.selectedFile) {

            return (
                <div>
                    <h2>File Details:</h2>
                    <p>File Name: {this.state.selectedFile.name}</p>
                    <p>File Type: {this.state.selectedFile.type}</p>
                </div>
            );
        } else {
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
                <div>
                    <input className='input-file' type="file" onChange={this.onFileChange} />
                    <button onClick={this.onFileUpload}>
                        Upload!
                    </button>
                </div>
                {this.fileData()}
            </div>
        );
    }
}