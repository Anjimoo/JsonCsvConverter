import React, { Component } from 'react';
import { TableRow } from './TableRow';
import './CsvRedactor.css';

export class CsvRedactor extends Component {
    static displayName = CsvRedactor.name;

    constructor(props) {
        super(props);
        this.state = {
            table: props.table
        }
        this.sendUpdatedTable = this.sendUpdatedTable.bind(this);
        this.downloadUpdatedJson = this.downloadUpdatedJson.bind(this);
    }

    handleUpdate = async (event, key) => {
        this.setState(prevState => {
            return {
                table: prevState.table.map((row) => {
                    return row.key === key ? { ...row, newValue: event.target.value } : row
                })
            }
        });
    }

    async sendUpdatedTable(event, key) {
        await fetch(`jsonconverter/update-json/${key}/${event.target.value}`,
            {
                method: 'POST',
                headers: {
                    'Accept': 'application/json',
                    'Content-Type': 'application/json'
                }
            }
        );
    }

    async downloadUpdatedJson() {
        await fetch(`jsonconverter/download-file`,
            {
                method: 'GET',
                headers: {
                    'Accept': 'application/json',
                    'Content-Type': 'application/json'
                }
            }
        ).then(response => response.json())
            .then(data => {
                let filename = "export.json";
                let contentType = "application/json;charset=utf-8;";
                if (window.navigator && window.navigator.msSaveOrOpenBlob) {
                    var blob = new Blob([decodeURIComponent(encodeURI(data))], { type: contentType });
                    navigator.msSaveOrOpenBlob(blob, filename);
                } else {
                    var a = document.createElement('a');
                    a.download = filename;
                    a.href = 'data:' + contentType + ',' + encodeURIComponent(data);
                    a.target = '_blank';
                    document.body.appendChild(a);
                    a.click();
                    document.body.removeChild(a);
                }
            });
}

componentWillReceiveProps(nextProps) {
    this.setState({
        table: nextProps.table
    });
}

render() {
    let isJsonUploaded = this.state.table === null;
    console.log(this.state.table);
    return (
        <div className='csv-redactor'>
            {isJsonUploaded ?
                <p>Upload Json to see CSV representation.</p>
                :
                <div className='edit-file-container'>
                    <h3>Edit json file</h3>
                    <div className='table-container'>
                        <table>
                            <thead>
                                <tr>
                                    <th>Key</th>
                                    <th>Value</th>
                                    <th>New Value</th>
                                </tr>
                            </thead>
                            <tbody>
                                {this.state.table.map((row) => (<TableRow key={row.key} row={row}
                                    handleUpdate={this.handleUpdate}
                                    sendUpdatedTable={this.sendUpdatedTable}
                                />))}
                            </tbody>
                        </table>
                    </div>
                    <button onClick={this.downloadUpdatedJson} className='down-button'><b>Download Edited Json</b></button>
                </div>
            }
        </div>
    );
}
}
